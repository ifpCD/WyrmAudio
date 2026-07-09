using UnityEngine;
using SteamAudio;
using Unity.Mathematics;

public partial class WyrmPhononSource : WyrmBaseSource
{
    public bool Reverb = false;
    public bool Ambisonics = false;

    // steam audio native wrapper
    private Source _phononSource;
    private int _pluginHandle = -1;

    private float _cachedOcclusion = -1f;
    private float _cachedTransMid = -1f;

    public override void Initialize(WyrmMixerPool pool)
    {
        base.Initialize(pool);

        if (SteamAudioManager.Simulator != null && (Reverb || Ambisonics))
        {
            var simSettings = SteamAudioManager.GetSimulationSettings(false);

            simSettings.flags = 0;
            if (Reverb) simSettings.flags |= SimulationFlags.Reflections;
            if (Ambisonics) simSettings.flags |= SimulationFlags.Pathing;

            _phononSource = new Source(SteamAudioManager.Simulator, simSettings);
            _phononSource.AddToSimulator(SteamAudioManager.Simulator);

            _pluginHandle = API.iplUnityAddSource(_phononSource.Get());
        }

        ASource.SetSpatializerFloat(DISTANCE_ATTENUATION, 1f);
        ASource.SetSpatializerFloat(AIR_ABSORPTION, 1f);
        ASource.SetSpatializerFloat(DIRECTIVITY, 0f);

        ASource.SetSpatializerFloat(OCCLUSION, 1f);
        ASource.SetSpatializerFloat(TRANSMISSION, 1f);

        ASource.SetSpatializerFloat(REFLECTIONS, Reverb ? 1f : 0f);
        ASource.SetSpatializerFloat(PATHING, Ambisonics ? 1f : 0f);

        ASource.SetSpatializerFloat(HRTF_INTERPOLATION, 1f); // 1 = bilinear

        ASource.SetSpatializerFloat(USER_DEFINED_DIRECTIVITY, 1f);
        ASource.SetSpatializerFloat(FREQUENCY_DEPENDENT_TRANSMISSION, 1f);

        ASource.SetSpatializerFloat(TRANSMISSION_LOW, 0.2f);
        ASource.SetSpatializerFloat(TRANSMISSION_MID, 0.2f);
        ASource.SetSpatializerFloat(TRANSMISSION_HIGH, 0.2f);

        ASource.SetSpatializerFloat(DIRECT_BINAURAL, 1f); // HRTF
        ASource.SetSpatializerFloat(PLUGIN_SOURCE_HANDLE, _pluginHandle); // we can disconnect from simulator if we pass -1
        ASource.SetSpatializerFloat(PERSPECTIVE_CORRECTION, 1f);
    }

    public override void Deactivate()
    {
        base.Deactivate();
        SetOcclusionAndTransmission(1f, 1f, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (_pluginHandle != -1)
            API.iplUnityRemoveSource(_pluginHandle);

        if (_phononSource != null)
        {
            if (SteamAudioManager.Simulator != null)
                _phononSource.RemoveFromSimulator(SteamAudioManager.Simulator);

            _phononSource.Release();
            _phononSource = null;
        }
    }

    public void UpdatePhononSimulatorPosition(float3 worldPos, float3 forward, float3 up, float3 right)
    {
        if (_phononSource == null) return;

        SimulationInputs inputs = new();

        inputs.source.origin = Common.ConvertVector(worldPos);
        inputs.source.ahead = Common.ConvertVector(forward);
        inputs.source.up = Common.ConvertVector(up);
        inputs.source.right = Common.ConvertVector(right);

        inputs.flags = 0;
        if (Reverb) inputs.flags |= SimulationFlags.Reflections;
        if (Ambisonics) inputs.flags |= SimulationFlags.Pathing;

        _phononSource.SetInputs(inputs.flags, inputs);
    }

    public void SetOcclusionAndTransmission(float occlusion, float transLow, float transMid, float transHigh)
    {
        if (Mathf.Abs(_cachedOcclusion - occlusion) > 0.01f)
        {
            ASource.SetSpatializerFloat(OCCLUSION_VALUE, occlusion);
            _cachedOcclusion = occlusion;
        }

        if (Mathf.Abs(_cachedTransMid - transMid) > 0.01f)
        {

            _cachedTransMid = transMid;
        }
    }
}