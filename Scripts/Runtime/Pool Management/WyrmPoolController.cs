using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class WyrmPoolController : MonoBehaviour
{
    // todo
    static readonly Dictionary<AudioMixerGroup, WyrmMixerGroupProcessor> processors = new();

    void Awake()
    {
        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            CreateMixerGroupManager(config);
        }
    }

    void LateUpdate()
    {
        foreach (var (_, manager) in processors)
        {
            manager.CullSources();
            manager.LateUpdateJobs();
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

    public static void Play(AudioMixerGroup mixerGroup, WyrmLoopableBank bank, Transform track = null, float? volume = null)
    {
        processors[mixerGroup].Play(bank, track, volume);
    }

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, Transform track = null, float? volume = null)
    {
        processors[mixerGroup].Play(clip, track, volume);
    }

    public static void PlayOneShot(AudioMixerGroup mixerGroup, AudioClip clip, Transform track = null, float? volume = null)
    {
        processors[mixerGroup].PlayOneShot(clip, track, volume);
    }

    public static void Play(AudioMixerGroup mixerGroup, WyrmLoopableBank bank, Vector3 position, float? volume = null)
    {
        processors[mixerGroup].Play(bank, position, volume);
    }

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, Vector3 position, float? volume = null)
    {
        processors[mixerGroup].Play(clip, position, volume);
    }

    public static void PlayOneShot(AudioMixerGroup mixerGroup, AudioClip clip, Vector3 position, float? volume = null)
    {
        processors[mixerGroup].PlayOneShot(clip, position, volume);
    }

    public static bool TryBorrow(AudioMixerGroup mixerGroup, out IWyrmSource pooledAudioSource)
    {
        return processors[mixerGroup].TryBorrow(out pooledAudioSource);
    }

    public static void Return(AudioMixerGroup mixerGroup, IWyrmSource pooledAudioSource)
    {
        processors[mixerGroup].Return(pooledAudioSource);
    }

    private void CreateMixerGroupManager(WyrmMixerGroupConfig config)
    {
        string mixerName = $"{config.targetMixerGroup.audioMixer.name} - {config.targetMixerGroup.name}";
        GameObject mixerManagerObject = new(mixerName);

        mixerManagerObject.transform.SetParent(transform);

        var mixerManager = mixerManagerObject.AddComponent<WyrmMixerGroupProcessor>();
        mixerManager.Initialize(config);

        processors[config.targetMixerGroup] = mixerManager;
    }
}
