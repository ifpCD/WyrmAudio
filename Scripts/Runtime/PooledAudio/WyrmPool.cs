using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class WyrmPool : MonoBehaviour
{
    static readonly Dictionary<AudioMixerGroup, WyrmMixerGroupManager> pools = new();

    void Awake()
    {
        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            CreateMixerGroupManager(config);
        }
    }

    void Update()
    {
        foreach (var (_, manager) in pools)
        {
            manager.CullSources();
            manager.UpdateChildrenTransforms();
        }
    }

    public static void Dispose()
    {
        pools.Clear();
    }

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        pools[mixerGroup].Play(clip, volume, trackedTransform);
    }

    public static void Play(AudioMixerGroup mixerGroup, WyrmAudioClip clip, float? volume = null, Transform trackedTransform = null, float? playbackLengthOverride = null)
    {
        pools[mixerGroup].Play(clip, volume, trackedTransform, playbackLengthOverride);
    }

    public static void PlayOneShot(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        pools[mixerGroup].PlayOneShot(clip, volume, trackedTransform);
    }

    public static bool TryBorrow(AudioMixerGroup mixerGroup, out IPooledAudioSource pooledAudioSource)
    {
        return pools[mixerGroup].TryBorrow(out pooledAudioSource);
    }

    public static void Return(AudioMixerGroup mixerGroup, IPooledAudioSource pooledAudioSource)
    {
        pools[mixerGroup].Return(pooledAudioSource);
    }

    private void CreateMixerGroupManager(WyrmMixerGroupConfig wyrmMixerConfig)
    {
        GameObject WyrmMixerManagerGameObject = new(wyrmMixerConfig.targetMixerGroup.name);
        WyrmMixerManagerGameObject.transform.SetParent(transform);

        var mixerManager = WyrmMixerManagerGameObject.AddComponent<WyrmMixerGroupManager>();
        mixerManager.Initialize(wyrmMixerConfig);

        pools[wyrmMixerConfig.targetMixerGroup] = mixerManager;
    }

    private void DestroyMixerGroupManager(AudioMixerGroup mixerGroup)
    {

    }
}
