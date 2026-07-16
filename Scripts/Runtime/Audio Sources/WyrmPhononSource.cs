using UnityEngine;
using SteamAudio;
using Unity.Mathematics;

public partial class WyrmPhononSource : WyrmBaseSource
{
    public Source PhononSource { get; private set; }
    private int _pluginHandle = -1;

    private float _cachedOcclusion = -1f;
    private float _cachedTransMid = -1f;

    public override void Initialize(WyrmMixerPool pool)
    {
        base.Initialize(pool);

        if (SteamAudioManager.Simulator != null && (UseReflections || UsePropagation))
        {
            var simSettings = SteamAudioManager.GetSimulationSettings(false);

            simSettings.flags = 0;
            if (UseReflections) simSettings.flags |= SimulationFlags.Reflections;

            // Phonon Source must initialize with the pathing flag
            // to allocate memory for eq/sh in C++. (otherwise we crash)
            // We then never send the Pathing flag ever again during simulator updates.
            if (UsePropagation) simSettings.flags |= SimulationFlags.Pathing;

            PhononSource = new Source(SteamAudioManager.Simulator, simSettings);

            PhononSource.AddToSimulator(SteamAudioManager.Simulator);
            _pluginHandle = API.iplUnityAddSource(PhononSource.Get());
        }

        ASource.SetSpatializerFloat(APPLY_DISTANCEATTENUATION, 1f);
        ASource.SetSpatializerFloat(APPLY_AIRABSORPTION, 1f);
        ASource.SetSpatializerFloat(APPLY_DIRECTIVITY, 0);
        ASource.SetSpatializerFloat(APPLY_OCCLUSION, 1f);
        ASource.SetSpatializerFloat(APPLY_TRANSMISSION, 0f);
        ASource.SetSpatializerFloat(APPLY_REFLECTIONS, UseReflections ? 1 : 0);
        ASource.SetSpatializerFloat(APPLY_PATHING, 1f);

        ASource.SetSpatializerFloat(HRTF_INTERPOLATION, 1f); // 1 = bilinear

        ASource.SetSpatializerFloat(DISTANCEATTENUATION, 1f);
        ASource.SetSpatializerFloat(DISTANCEATTENUATION_USECURVE, 0f);
        
        // ASource.SetSpatializerFloat(DISTANCEATTENUATION_USECURVE, 0f);
        // ASource.SetSpatializerFloat(DISTANCEATTENUATION_USECURVE, 0f);
        // ASource.SetSpatializerFloat(DISTANCEATTENUATION_USECURVE, 0f);

        ASource.SetSpatializerFloat(DIRECTIVITY, 1f);
        ASource.SetSpatializerFloat(TRANSMISSION_TYPE, 1f);

        ASource.SetSpatializerFloat(TRANSMISSION_LOW, 0.1f);
        ASource.SetSpatializerFloat(TRANSMISSION_MID, 0.025f);
        ASource.SetSpatializerFloat(TRANSMISSION_HIGH, 0.025f);

        // ASource.SetSpatializerFloat(DIRECT_MIXLEVEL, 0);

        // ASource.SetSpatializerFloat(REFLECTIONS_BINAURAL, 1);
        // ASource.SetSpatializerFloat(REFLECTIONS_MIXLEVEL, 10);
        // ASource.SetSpatializerFloat(PATHING_MIXLEVEL, 0);



        // ASource.SetSpatializerFloat(PATHING_BINAURAL, 1f); // HRTF Propagation

        ASource.SetSpatializerFloat(DIRECT_BINAURAL, 1f); // HRTF
        ASource.SetSpatializerFloat(SIMULATION_OUTPUTS_HANDLE, _pluginHandle); // we can disconnect from simulator if we pass -1
        ASource.SetSpatializerFloat(PERSPECTIVE_CORRECTION, 1f);


        // ASource.SetSpatializerFloat(NORMALIZE_PATHING_EQ, 1f); // we explode without this when we feed 0,0,0 propagation eq

        UpdatePhononSimulator();
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }

    private void OnDestroy()
    {
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
        if (PhononSource == null) return;

        SimulationInputs inputs = new() { flags = 0 };
        if (UseReflections) inputs.flags |= SimulationFlags.Reflections;

        PhononSource.SetInputs(inputs.flags, inputs);
    }

    // Candidate for custom batch api
    public void SetOcclusionLevel(float occlusion)
    {
        if (Mathf.Abs(_cachedOcclusion - occlusion) > 0.01f)
        {
            ASource.SetSpatializerFloat(OCCLUSION, occlusion);
            ASource.SetSpatializerFloat(REFLECTIONS_MIXLEVEL, occlusion);
            _cachedOcclusion = occlusion;
        }
    }
}