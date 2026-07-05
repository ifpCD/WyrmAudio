using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class WyrmPool : MonoBehaviour
{
    readonly Dictionary<AudioMixerGroup, WyrmMixerGroupManager> pools = new();
    Dictionary<AudioMixerGroup, WyrmMixerGroupConfig> mixerGroupConfigs;

    private

    void Start()
    {
        DontDestroyOnLoad(this);
        foreach (var (mixerGroup, wyrmMixerConfig) in mixerGroupConfigs)
        {
            CreateMixerGroupManager(mixerGroup, wyrmMixerConfig);
        }
    }

    void Update()
    {

    }

    public void Play(AudioMixerGroup mixerGroup, AudioClip clip, float volume)
    {
        pools[mixerGroup].Play(clip, volume);
    }

    public void Play(AudioMixerGroup mixerGroup, AudioClip clip, float volume = default, Transform transform = default)
    {
        pools[mixerGroup].Play(clip, volume, transform);
    }

    public bool TryBorrow(AudioMixerGroup mixerGroup, out IPooledAudioSource pooledAudioSource)
    {
        return pools[mixerGroup].TryBorrow(out pooledAudioSource);
    }

    public void Return(AudioMixerGroup mixerGroup, IPooledAudioSource pooledAudioSource)
    {
        pools[mixerGroup].Return(pooledAudioSource);
    }

    private void CreateMixerGroupManager(AudioMixerGroup mixerGroup, WyrmMixerGroupConfig wyrmMixerConfig)
    {
        GameObject WyrmMixerManagerGameObject = new(mixerGroup.name);
        WyrmMixerManagerGameObject.transform.SetParent(transform);

        var mixerManager = WyrmMixerManagerGameObject.AddComponent<WyrmMixerGroupManager>();
        mixerManager.config = wyrmMixerConfig;
    }

    private void DestroyMixerGroupManager(AudioMixerGroup mixerGroup)
    {

    }
}
