using UnityEngine;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    private IPooledAudioSource[] _availableSources;
    private int _availableCount;

    private IPooledAudioSource[] _activeSources;
    private int _activeCount;

    private int _totalCreated;

    void Start()
    {
        if (config.targetMixerGroup == null || config.WyrmAudioSourcePrefab == null)
        {
#if !DEVELOPMENT_BUILD
            Debug.LogError("Incomplete Mixer Group Config");
#endif
            return;
        }

        _availableSources = new IPooledAudioSource[config.maxSize];
        _activeSources = new IPooledAudioSource[config.maxSize];

        for (int i = 0; i < config.initialSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    internal void CullSources()
    {
        for (int index = _activeCount - 1; index >= 0; index--)
        {
            if (!_activeSources[index].IsPlaying)
            {
                ReturnActiveSourceAtIndex(index);
            }
        }
    }

    internal void UpdateChildrenTransforms()
    {
        for (int index = 0; index < _activeCount; index++)
        {
            var activeSource = _activeSources[index];
            if (activeSource.TrackedTransform != null)
            {
                activeSource.gameObject.transform.SetPositionAndRotation(
                    activeSource.TrackedTransform.position,
                    activeSource.TrackedTransform.rotation);
            }
        }
    }

    public void Play(AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (trackedTransform != null) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (trackedTransform != null) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.PlayOneShot(clip);
    }

    public bool TryBorrow(out IPooledAudioSource pooledAudioSource)
    {
        pooledAudioSource = null;

        if (_availableCount == 0)
        {
            if (_totalCreated >= config.maxSize)
            {
#if !DEVELOPMENT_BUILD
                Debug.LogWarning($"Mixer Group {config.targetMixerGroup.name} is overflowing, skipping audio play.");
#endif
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

    void ReturnActiveSourceAtIndex(int index)
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

    void CreatePooledAudioSource()
    {
        if (_totalCreated >= config.maxSize) return;

        GameObject newPooledAudioSourceGO = Instantiate(config.WyrmAudioSourcePrefab, transform);
        if (!newPooledAudioSourceGO.TryGetComponent(out IPooledAudioSource pooledAudioSource))
        {
#if !DEVELOPMENT_BUILD
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IPooledAudioSource type component");
#endif
            return;
        }

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}