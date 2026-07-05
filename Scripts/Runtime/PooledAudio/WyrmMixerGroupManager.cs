using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    readonly Queue<IPooledAudioSource> available = new();
    readonly HashSet<IPooledAudioSource> active = new();

    void Start() => CreatePool();

    void Update()
    {
        foreach (var activeSource in active)
        {
            if (!activeSource.IsPlaying)
                Return(activeSource);
        }
    }

    public void Play(AudioClip clip, float volume = default, Transform parentTransform = default)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.SetClip(clip);
        if (volume != default) borrowedSource.SetVolume(volume);
        if (parentTransform != default) borrowedSource.transform.SetParent(parentTransform);

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float volume = default, Transform parentTransform = default)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.SetClip(clip);
        if (volume != default) borrowedSource.SetVolume(volume);
        if (parentTransform != default) borrowedSource.transform.SetParent(parentTransform);

        borrowedSource.Play();

        ScheduleReturnAsync(borrowedSource, clip).Forget();
    }

    public bool TryBorrow(out IPooledAudioSource pooledAudioSource)
    {
        pooledAudioSource = null;

        if (available.Count == 0)
        {
            if (IsOverflowing) return false;
            CreatePooledAudioSource();
        }

        pooledAudioSource = available.Dequeue();
        active.Add(pooledAudioSource);

        pooledAudioSource.PlayVersion++;

        return true;
    }

    public void Return(IPooledAudioSource pooledAudioSource)
    {
        if (!active.Remove(pooledAudioSource)) return;

        pooledAudioSource.Stop();
        pooledAudioSource.TrackedTransform = null;

        pooledAudioSource.PlayVersion++;

        available.Enqueue(pooledAudioSource);
    }

    private async UniTaskVoid ScheduleReturnAsync(IPooledAudioSource source, AudioClip clip)
    {
        int currentVersion = source.PlayVersion;

        float pitch = Mathf.Abs(source.Pitch);
        float duration = pitch > 0.01f ? (clip.length / pitch) : clip.length;

        bool isDestroyed = await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            ignoreTimeScale: true,
            cancellationToken: source.gameObject.GetCancellationTokenOnDestroy()
        ).SuppressCancellationThrow();

        if (isDestroyed) return;

        if (source.PlayVersion == currentVersion)
        {
            Return(source);
        }
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
        {
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IPooledAudioSource type component");
            return;
        }

        available.Enqueue(pooledAudioSource);
    }

    bool IsOverflowing
    {
        get
        {
            if (active.Count >= config.maxSize)
            {
                if (config.warnOverflow)
                    Debug.LogWarning("Mixer Group is overflowing, skipping audio play");
                return true;
            }
            return false;
        }
    }
}