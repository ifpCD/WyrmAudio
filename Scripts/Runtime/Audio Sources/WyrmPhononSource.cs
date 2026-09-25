using SteamAudio;
using UnityEngine;

public sealed partial class WyrmPhononSource : WyrmBaseSource
{
    const int INACTIVE_SPATIALIZER_POINTER = -1;
    int _pluginHandle = INACTIVE_SPATIALIZER_POINTER;

    internal Source PhononSource { get; private set; }

    public override float OcclusionValue
    {
        set
        {
            if (_occlusionValue == value)
                return;

            _occlusionValue = value;
            SetSpatialValue(OCCLUSION, value);
        }
    }

    void SetSpatialValue(int index, float value) => ASource.SetSpatializerFloat(index, value);

    protected override void Awake()
    {
        base.Awake();

        if (SteamAudioManager.Simulator == null)
            return;

        var simSettings = SteamAudioManager.GetSimulationSettings(false);

        simSettings.flags = 0;
        if (UseReflections)
            simSettings.flags |= SimulationFlags.Reflections;

        // Phonon Source must initialize with the pathing flag
        // to allocate memory for eq/sh in C++. (otherwise we crash)
        // We then never send the Pathing flag ever again during simulator updates.
        if (UseAmbisonics)
            simSettings.flags |= SimulationFlags.Pathing;

        PhononSource = new Source(SteamAudioManager.Simulator, simSettings);

        PhononSource.AddToSimulator(SteamAudioManager.Simulator);
        _pluginHandle = API.iplUnityAddSource(PhononSource.Get());

        SetSpatialValue(APPLY_DISTANCEATTENUATION, 1f);
        SetSpatialValue(APPLY_AIRABSORPTION, 1f);
        SetSpatialValue(APPLY_DIRECTIVITY, FALSE);
        SetSpatialValue(APPLY_OCCLUSION, UseOcclusion.ToFloat());
        SetSpatialValue(APPLY_TRANSMISSION, FALSE);
        SetSpatialValue(APPLY_REFLECTIONS, UseReflections.ToFloat());
        SetSpatialValue(APPLY_PATHING, UseAmbisonics.ToFloat());

        SetSpatialValue(HRTF_INTERPOLATION, 1f);

        SetSpatialValue(DISTANCEATTENUATION, 1f);
        SetSpatialValue(DISTANCEATTENUATION_USECURVE, 0f);

        SetSpatialValue(TRANSMISSION_TYPE, 1f);

        SetSpatialValue(TRANSMISSION_LOW, 0.1f);
        SetSpatialValue(TRANSMISSION_MID, 0.025f);
        SetSpatialValue(TRANSMISSION_HIGH, 0.025f);

        SetSpatialValue(DIRECT_MIXLEVEL, 0f);

        SetSpatialValue(REFLECTIONS_BINAURAL, 1);
        SetSpatialValue(REFLECTIONS_MIXLEVEL, 10);
        SetSpatialValue(PATHING_MIXLEVEL, 0f);

        SetSpatialValue(PATHING_BINAURAL, TRUE); // HRTF Propagation

        SetSpatialValue(DIRECT_BINAURAL, TRUE); // HRTF
        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, _pluginHandle); // we can disconnect from simulator if we pass -1
        // SetSpatialValue(PERSPECTIVE_CORRECTION, 1f);

        // SetSpatialValue(NORMALIZE_PATHING_EQ, 1f);

        UpdatePhononSimulator();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_pluginHandle != INACTIVE_SPATIALIZER_POINTER)
            API.iplUnityRemoveSource(_pluginHandle);

        if (PhononSource == null)
            return;

        if (SteamAudioManager.Simulator != null)
            PhononSource.RemoveFromSimulator(SteamAudioManager.Simulator);

        PhononSource.Release();
        PhononSource = null;
    }

    // void Update() => UpdatePhononSimulator();
    // void LateUpdate() => UpdatePhononSimulator();

    // Candidate for custom batch api
    public void UpdatePhononSimulator()
    {
        if (PhononSource == null)
            return;

        SimulationInputs inputs = new() { flags = 0 };
        if (UseReflections)
            inputs.flags |= SimulationFlags.Reflections;

        PhononSource.SetInputs(inputs.flags, inputs);
    }

    public override void Play()
    {
        ASource.enabled = true;
        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, _pluginHandle);

        base.Play();
    }

    public override void PlayOneShot(AudioClip clip)
    {
        ASource.enabled = true;
        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, _pluginHandle);

        base.PlayOneShot(clip);
    }

    internal override bool Deactivate()
    {
        if (!base.Deactivate())
            return false;

        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, INACTIVE_SPATIALIZER_POINTER);
        ASource.enabled = false;

        return true;
    }
}
