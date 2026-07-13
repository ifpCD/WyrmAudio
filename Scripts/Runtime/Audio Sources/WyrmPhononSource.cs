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

        ASource.SetSpatializerFloat(DISTANCE_ATTENUATION, 1f);
        ASource.SetSpatializerFloat(AIR_ABSORPTION, 1f);
        ASource.SetSpatializerFloat(DIRECTIVITY, 0f);

        ASource.SetSpatializerFloat(OCCLUSION, 1f);
        ASource.SetSpatializerFloat(TRANSMISSION, 1f);

        ASource.SetSpatializerFloat(REFLECTIONS, UseReflections ? 1f : 0f);
        ASource.SetSpatializerFloat(PATHING, 1f);

        ASource.SetSpatializerFloat(HRTF_INTERPOLATION, 1f); // 1 = bilinear

        ASource.SetSpatializerFloat(USER_DEFINED_DIRECTIVITY, 1f);
        ASource.SetSpatializerFloat(FREQUENCY_DEPENDENT_TRANSMISSION, 1f);

        ASource.SetSpatializerFloat(TRANSMISSION_LOW, 0.2f);
        ASource.SetSpatializerFloat(TRANSMISSION_MID, 0.05f);
        ASource.SetSpatializerFloat(TRANSMISSION_HIGH, 0.05f);

        ASource.SetSpatializerFloat(DIRECT_BINAURAL, 1f); // HRTF
        ASource.SetSpatializerFloat(PLUGIN_SOURCE_HANDLE, _pluginHandle); // we can disconnect from simulator if we pass -1
        ASource.SetSpatializerFloat(PERSPECTIVE_CORRECTION, 1f);
    }

    public override void Deactivate()
    {
        base.Deactivate();
        SetOcclusionLevel(1f);
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
    public void UpdatePhononSimulator(float3 worldPos)
    {
        if (PhononSource == null) return;

        SimulationInputs inputs = new();

        inputs.source.origin = Common.ConvertVector(worldPos);
        inputs.source.ahead = Common.ConvertVector(float3.zero);
        inputs.source.up = Common.ConvertVector(float3.zero);
        inputs.source.right = Common.ConvertVector(float3.zero);

        inputs.flags = 0;
        if (UseReflections) inputs.flags |= SimulationFlags.Reflections;

        PhononSource.SetInputs(inputs.flags, inputs);
    }

    // Candidate for custom batch api
    public void SetOcclusionLevel(float occlusion)
    {
        if (Mathf.Abs(_cachedOcclusion - occlusion) > 0.01f)
        {
            ASource.SetSpatializerFloat(OCCLUSION_VALUE, occlusion);
            _cachedOcclusion = occlusion;
        }
    }
}