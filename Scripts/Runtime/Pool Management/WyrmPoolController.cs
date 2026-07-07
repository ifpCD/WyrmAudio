using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class WyrmPoolController : MonoBehaviour
{
    static readonly Dictionary<AudioMixerGroup, WyrmMixerGroupProcessor> processors = new();

    public static float DspDeltaTime { get; private set; }
    private static double _lastDspTime;
    private static bool _dspInitialized;

    void Awake()
    {
        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            CreateMixerGroupManager(config);
        }
    }

    void Update()
    {
        double currentDspTime = AudioSettings.dspTime;
        if (!_dspInitialized)
        {
            _lastDspTime = currentDspTime;
            _dspInitialized = true;
        }

        DspDeltaTime = (float)(currentDspTime - _lastDspTime);
        _lastDspTime = currentDspTime;

        foreach (var (_, manager) in processors)
        {
            manager.CullSources();
            manager.UpdateJobs();
        }
    }

    void LateUpdate()
    {
        foreach (var (_, manager) in processors)
        {
            manager.LateUpdateComplete();
        }
    }

    public static void Dispose()
    {
        foreach (var pool in processors.Values)
        {
            if (pool != null && pool.gameObject != null)
            {
                Destroy(pool.gameObject);
            }
        }
        processors.Clear();
    }

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform track = null)
    {
        processors[mixerGroup].Play(clip, volume, track);
    }

    public static void PlayOneShot(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform track = null)
    {
        processors[mixerGroup].PlayOneShot(clip, volume, track);
    }

    public static bool TryBorrow(AudioMixerGroup mixerGroup, out IWyrmSource pooledAudioSource)
    {
        return processors[mixerGroup].TryBorrow(out pooledAudioSource);
    }

    public static void Return(AudioMixerGroup mixerGroup, IWyrmSource pooledAudioSource)
    {
        processors[mixerGroup].Return(pooledAudioSource);
    }

    private void CreateMixerGroupManager(WyrmMixerGroupConfig wyrmMixerConfig)
    {
        GameObject WyrmMixerManagerGameObject = new($"{wyrmMixerConfig.targetMixerGroup.audioMixer.name} - {wyrmMixerConfig.targetMixerGroup.name}");
        WyrmMixerManagerGameObject.transform.SetParent(transform);

        var mixerManager = WyrmMixerManagerGameObject.AddComponent<WyrmMixerGroupProcessor>();
        mixerManager.Initialize(wyrmMixerConfig);

        processors[wyrmMixerConfig.targetMixerGroup] = mixerManager;
    }

    private void DestroyMixerGroupManager(AudioMixerGroup mixerGroup)
    {

    }
}
