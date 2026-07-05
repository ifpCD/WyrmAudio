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
        _availableSources = new IPooledAudioSource[config.maxSize];
        _activeSources = new IPooledAudioSource[config.maxSize];

        for (int i = 0; i < config.initialSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    public void CullSources()
    {
        for (int index = _activeCount - 1; index >= 0; index--)
        {
            if (!_activeSources[index].IsPlaying)
            {
                ReturnActiveSourceAtIndex(index);
            }
        }
    }

    public void UpdateChildrenTransforms()
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

        borrowedSource.SetClip(clip);
        if (volume.HasValue) borrowedSource.SetVolume(volume.Value);
        if (trackedTransform != null) borrowedSource.TrackedTransform = trackedTransform;

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform trackedTransform = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.SetVolume(volume.Value);
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