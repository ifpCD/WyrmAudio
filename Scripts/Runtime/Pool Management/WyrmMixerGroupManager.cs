using UnityEngine;

public class WyrmMixerGroupManager : MonoBehaviour
{
    public WyrmMixerGroupConfig config;

    private IWyrmSource[] _availableSources;
    private int _availableCount;

    private IWyrmSource[] _activeSources;
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

        _availableSources = new IWyrmSource[this.config.maxSize];
        _activeSources = new IWyrmSource[this.config.maxSize];

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
            var trackedTransform = source.TrackedTransform;

            // Todo: maybe stop playing here?
            if (trackedTransform == null) continue;

            trackedTransform.GetPositionAndRotation(out Vector3 currentPos, out Quaternion currentRot);

            if (source.CachedPosition != currentPos || source.CachedRotation != currentRot)
            {
                source.CachedPosition = currentPos;
                source.CachedRotation = currentRot;

                source.BaseTransform.SetPositionAndRotation(currentPos, currentRot);
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

    public void Play(WyrmAudioClip clip, float? volume = null, Transform trackedTransform = null, float? playbackLengthOverride = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (trackedTransform != null) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.Play(clip, playbackLengthOverride);
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (trackedTransform != null) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.PlayOneShot(clip);
    }

    public bool TryBorrow(out IWyrmSource pooledAudioSource)
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

    public void Return(IWyrmSource pooledAudioSource)
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
        if (!newPooledAudioSourceGO.TryGetComponent(out IWyrmSource pooledAudioSource))
        {
#if UNITY_EDITOR
            Debug.LogError("Pooled Audio Source Prefab doesn't contain IWyrmSource type component");
#endif
            return;
        }

        pooledAudioSource.SetConfig(config);

        _availableSources[_availableCount++] = pooledAudioSource;
        _totalCreated++;
    }
}