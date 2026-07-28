using SteamAudio;
using UnityEngine;

public sealed partial class WyrmPhononSource : WyrmBaseSource
{
    public Source PhononSource { get; private set; }
    private int _pluginHandle = -1;

    private float _cachedOcclusion = -1f;

    void SetSpatialValue(int index, float value) => ASource.SetSpatializerFloat(index, value);

    protected override void Awake()
    {
        base.Awake();

        if (SteamAudioManager.Simulator != null && (UseReflections || UsePropagation))
        {
            var simSettings = SteamAudioManager.GetSimulationSettings(false);

            simSettings.flags = 0;
            if (UseReflections)
                simSettings.flags |= SimulationFlags.Reflections;

            // Phonon Source must initialize with the pathing flag
            // to allocate memory for eq/sh in C++. (otherwise we crash)
            // We then never send the Pathing flag ever again during simulator updates.
            if (UsePropagation)
                simSettings.flags |= SimulationFlags.Pathing;

            PhononSource = new Source(SteamAudioManager.Simulator, simSettings);

            PhononSource.AddToSimulator(SteamAudioManager.Simulator);
            _pluginHandle = API.iplUnityAddSource(PhononSource.Get());
        }

        SetSpatialValue(APPLY_DISTANCEATTENUATION, 1f);
        SetSpatialValue(APPLY_AIRABSORPTION, 1f);
        SetSpatialValue(APPLY_DIRECTIVITY, 0);
        SetSpatialValue(APPLY_OCCLUSION, 1f);
        SetSpatialValue(APPLY_TRANSMISSION, 0f);
        SetSpatialValue(APPLY_REFLECTIONS, UseReflections ? 1 : 0);
        SetSpatialValue(APPLY_PATHING, 1f);

        SetSpatialValue(HRTF_INTERPOLATION, 1f); // 1 = bilinear

        SetSpatialValue(DISTANCEATTENUATION, 1f);
        SetSpatialValue(DISTANCEATTENUATION_USECURVE, 0f);

        // SetSpatialValue(DISTANCEATTENUATION_USECURVE, 0f);
        // SetSpatialValue(DISTANCEATTENUATION_USECURVE, 0f);
        // SetSpatialValue(DISTANCEATTENUATION_USECURVE, 0f);

        SetSpatialValue(DIRECTIVITY, 1f);
        SetSpatialValue(TRANSMISSION_TYPE, 1f);

        SetSpatialValue(TRANSMISSION_LOW, 0.1f);
        SetSpatialValue(TRANSMISSION_MID, 0.025f);
        SetSpatialValue(TRANSMISSION_HIGH, 0.025f);

        // SetSpatialValue(DIRECT_MIXLEVEL, 0);

        // SetSpatialValue(REFLECTIONS_BINAURAL, 1);
        // SetSpatialValue(REFLECTIONS_MIXLEVEL, 10);
        // SetSpatialValue(PATHING_MIXLEVEL, 0);

        // SetSpatialValue(PATHING_BINAURAL, 1f); // HRTF Propagation

        SetSpatialValue(DIRECT_BINAURAL, 1f); // HRTF
        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, _pluginHandle); // we can disconnect from simulator if we pass -1
        SetSpatialValue(PERSPECTIVE_CORRECTION, 1f);

        // SetSpatialValue(NORMALIZE_PATHING_EQ, 1f); // we explode without this when we feed 0,0,0 propagation eq

        UpdatePhononSimulator();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_pluginHandle != -1)
            API.iplUnityRemoveSource(_pluginHandle);

        if (PhononSource != null)
        {
            if (SteamAudioManager.Simulator != null)
                PhononSource.RemoveFromSimulator(SteamAudioManager.Simulator);

            PhononSource.Release();
            PhononSource = null;
        }
    }

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

    // Candidate for custom batch api
    public void SetOcclusionLevel(float occlusion)
    {
        if (Mathf.Abs(_cachedOcclusion - occlusion) > 0.01f)
        {
            SetSpatialValue(OCCLUSION, occlusion);
            SetSpatialValue(REFLECTIONS_MIXLEVEL, occlusion);
            _cachedOcclusion = occlusion;
        }
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

        SetSpatialValue(SIMULATION_OUTPUTS_HANDLE, -1f);
        ASource.enabled = false;

        return true;
    }
}
