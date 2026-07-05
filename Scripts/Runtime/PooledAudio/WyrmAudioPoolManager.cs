using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum PooledAudioSourceType
{
    Ambient
}

public class WyrmAudioPoolManager : MonoBehaviour
{
    Dictionary<AudioMixerGroup, List<PooledAudioSource>> pooledAudioSources = new();

    void Start()
    {
        DontDestroyOnLoad(this);

    }

    void Update()
    {

    }

    public void Play(AudioClip clip, AudioMixer mixer, float volume) { }

    public PooledAudioSource Borrow()
    {
        return new PooledAudioSource();
    }

    public PooledAudioSource Return()
    {
        return new PooledAudioSource();
    }

    private void CreateAudioPool(AudioGroupPoolingConfig poolingConfig)
    {

    }

    private void DestroyAudioPool(AudioGroupPoolingConfig poolingConfig)
    {

    }
}
