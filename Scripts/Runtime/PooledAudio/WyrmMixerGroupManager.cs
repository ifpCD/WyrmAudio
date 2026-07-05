using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    // Fixed-length arrays replace dynamic collections
    private IPooledAudioSource[] _availableSources;
    private int _availableCount;

    private IPooledAudioSource[] _activeSources;
    private int _activeCount;

    private int _totalCreated;

    void Start()
    {
        // Pre-allocate arrays to the absolute maximum capacity
        _availableSources = new IPooledAudioSource[config.maxSize];
        _activeSources = new IPooledAudioSource[config.maxSize];

        for (int i = 0; i < config.initialSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    public void CullSources()
    {
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            var activeSource = _activeSources[i];
            if (!activeSource.IsPlaying && !activeSource.IsOneShot)
            {
                ReturnActiveSourceAtIndex(i);
            }
        }
    }

    public void UpdateChildrenTransforms()
    {
        for (int i = 0; i < _activeCount; i++)
        {
            var activeSource = _activeSources[i];
            if (activeSource.TrackedTransform != null)
            {
                activeSource.gameObject.transform.SetPositionAndRotation(
                    activeSource.TrackedTransform.position,
                    activeSource.TrackedTransform.rotation);
            }
        }
    }

    public void Play(AudioClip clip, float volume = default, Transform trackedTransform = default)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.SetClip(clip);
        if (volume != default) borrowedSource.SetVolume(volume);
        if (trackedTransform != default) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float volume = default, Transform trackedTransform = default)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume != default) borrowedSource.SetVolume(volume);
        if (trackedTransform != default) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.PlayOneShot(clip);

        ScheduleReturnAsync(borrowedSource, clip).Forget();
    }

    public bool TryBorrow(out IPooledAudioSource pooledAudioSource)
    {
        pooledAudioSource = null;

        if (_availableCount == 0)
        {
            if (_totalCreated >= config.maxSize)
            {
                if (config.warnOverflow)
                    Debug.LogWarning($"Mixer Group {config.targetMixerGroup?.name} is overflowing, skipping audio play.");
                return false;
            }
            CreatePooledAudioSource();
        }

        // Pop from available
        pooledAudioSource = _availableSources[--_availableCount];
        _availableSources[_availableCount] = null;

        // Push to active
        _activeSources[_activeCount++] = pooledAudioSource;

        pooledAudioSource.PlayVersion++;

        return true;
    }

    public void Return(IPooledAudioSource pooledAudioSource)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            if (_activeSources[i] == pooledAudioSource)
            {
                ReturnActiveSourceAtIndex(i);
                return;
            }
        }
    }

    private void ReturnActiveSourceAtIndex(int index)
    {
        var source = _activeSources[index];

        _activeCount--;
        if (index < _activeCount)
        {
            _activeSources[index] = _activeSources[_activeCount];
        }
        
        _activeSources[_activeCount] = null; 

        source.Stop();
        source.TrackedTransform = null;
        source.PlayVersion++;

        _availableSources[_availableCount++] = source;
    }

    private async UniTaskVoid ScheduleReturnAsync(IPooledAudioSource source, AudioClip clip)
    {
        int preAwaitedVersion = source.PlayVersion;

        float pitch = Mathf.Abs(source.Pitch);
        float duration = pitch > 0.01f ? (clip.length / pitch) : clip.length;

        bool isDestroyed = await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            ignoreTimeScale: true,
            cancellationToken: source.gameObject.GetCancellationTokenOnDestroy()
        ).SuppressCancellationThrow();

        if (isDestroyed) return;

        if (source.PlayVersion == preAwaitedVersion)
        {
            Return(source);
        }
    }

    private void CreatePooledAudioSource()
    {
        if (_totalCreated >= config.maxSize) return;

        GameObject newPooledAudioSourceGO = Instantiate(config.WyrmAudioSourcePrefab, transform);
        if (!newPooledAudioSourceGO.TryGetComponent(out IPooledAudioSource pooledAudioSource))
        {
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IPooledAudioSource type component");
            return;
        }

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}