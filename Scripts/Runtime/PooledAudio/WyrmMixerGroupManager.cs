using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    readonly Queue<IPooledAudioSource> available = new();
    readonly HashSet<IPooledAudioSource> active = new();

    void Start() => CreatePool();

    public void Play(AudioClip clip, float volume = default)
    {
        if (available.Count == 0)
        {
            if (IsOverflowing)
                return;

            CreatePooledAudioSource();
        }

        var borrowedSource = available.Dequeue();
        borrowedSource.SetClip(clip);
        borrowedSource.SetVolume(volume);
        borrowedSource.Play();
    }

    public bool TryBorrow(out IPooledAudioSource pooledAudioSource)
    {
        pooledAudioSource = null;
        if (IsOverflowing)
            return false;

        pooledAudioSource = available.Dequeue();
        return true;
    }

    public void Return(IPooledAudioSource pooledAudioSource)
    {
        available.Enqueue(pooledAudioSource);
    }

    private void CreatePool()
    {
        for (int i = 0; i < config.initialSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    private void CreatePooledAudioSource()
    {
        GameObject newPooledAudioSourceGO = Instantiate(config.WyrmAudioSourcePrefab, transform);
        if (!newPooledAudioSourceGO.TryGetComponent(out IPooledAudioSource pooledAudioSource))
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IPooledAudioSource type component");

        available.Append(pooledAudioSource);
    }

    bool IsOverflowing
    {
        get
        {
            if (active.Count == config.maxSize)
            {
                if (config.warnOverflow)
                    Debug.LogWarning("Mixer Group is overflowing, skipping audio play");

                return true;
            }
            return false;
        }
    }

    private void DestroyPool(AudioMixerGroup mixerGroup)
    {

    }
}
