using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class WyrmAudioPoolManager : MonoBehaviour
{
    readonly Dictionary<AudioMixerGroup, WyrmMixerManager> pools = new();
    Dictionary<AudioMixerGroup, WyrmMixerGroupConfig> mixerGroupConfigs;

    private

    void Start()
    {
        DontDestroyOnLoad(this);
        foreach (var (mixerGroup, wyrmMixerConfig) in mixerGroupConfigs)
        {
            CreateMixerGroupAudioPool(mixerGroup, wyrmMixerConfig);
        }
    }

    void Update()
    {

    }

    public void Play(AudioClip clip, AudioMixerGroup mixer, float volume)
    {

    }

    public PooledAudioSource Borrow(AudioMixerGroup audioMixerGroup)
    {
        return new PooledAudioSource();
    }

    public void Return(AudioMixerGroup audioMixerGroup)
    {

    }

    private void CreateMixerGroupAudioPool(AudioMixerGroup mixerGroup, WyrmMixerGroupConfig wyrmMixerConfig)
    {
        GameObject WyrmMixerManagerGameObject = new(mixerGroup.name);
        WyrmMixerManagerGameObject.transform.SetParent(transform);

        var mixerManager = WyrmMixerManagerGameObject.AddComponent<WyrmMixerManager>();
        mixerManager.config = wyrmMixerConfig;
    }

    private void DestroyMixerGroupAudioPool(AudioMixerGroup mixerGroup)
    {

    }
}
