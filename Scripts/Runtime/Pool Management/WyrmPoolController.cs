using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class WyrmPoolController : MonoBehaviour
{
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

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform track = null)
    {
        processors[mixerGroup].Play(clip, volume, track);
    }

    public static void PlayOneShot(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform track = null)
    {
        processors[mixerGroup].PlayOneShot(clip, volume, track);
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
