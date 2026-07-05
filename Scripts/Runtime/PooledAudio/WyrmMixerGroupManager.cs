using UnityEngine;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    private IPooledAudioSource[] _availableSources;
    private int _availableCount;

    private IPooledAudioSource[] _activeSources;
    private int _activeCount;

    private int _totalCreated;

    public void Initialize(WyrmMixerGroupConfig config)
    {
        this.config = config;

        if (this.config.targetMixerGroup == null || this.config.WyrmAudioSourcePrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Incomplete Mixer Group Config");
#endif
            return;
        }

        _availableSources = new IPooledAudioSource[this.config.maxSize];
        _activeSources = new IPooledAudioSource[this.config.maxSize];

        for (int i = 0; i < this.config.initialSize; i++)
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
        if (config.isNonSpatial) return;

        var sources = _activeSources;
        for (int i = 0; i < _activeCount; i++)
        {
            var source = sources[i];
            var t = source.TrackedTransform;

            if (t != null) source.BaseTransform.SetPositionAndRotation(t.position, t.rotation);
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
#if UNITY_EDITOR
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

        source.Deactivate();

        _availableSources[_availableCount++] = source;
    }

    void CreatePooledAudioSource()
    {
        if (_totalCreated >= config.maxSize) return;

        GameObject newPooledAudioSourceGO = Instantiate(config.WyrmAudioSourcePrefab, transform);
        if (!newPooledAudioSourceGO.TryGetComponent(out IPooledAudioSource pooledAudioSource))
        {
#if UNITY_EDITOR
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IPooledAudioSource type component");
#endif
            return;
        }

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}