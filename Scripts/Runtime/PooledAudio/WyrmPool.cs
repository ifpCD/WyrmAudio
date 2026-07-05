using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class WyrmPool : MonoBehaviour
{
    public static Dictionary<AudioMixerGroup, WyrmMixerGroupManager> pools = new();
    List<WyrmMixerGroupConfig> configs = new();

    void Awake()
    {
        foreach(var config in configs) CreateMixerGroupManager(config);
    }

    void Update()
    {
        foreach (var (_, mixerManager) in pools)
        {
            mixerManager.CullSources();
            mixerManager.UpdateChildrenTransforms();
        }
    }

    public static void Play(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        pools[mixerGroup].Play(clip, volume, trackedTransform);
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
        mixerManager.config = wyrmMixerConfig;
    }

    private void DestroyMixerGroupManager(AudioMixerGroup mixerGroup)
    {

    }
}
