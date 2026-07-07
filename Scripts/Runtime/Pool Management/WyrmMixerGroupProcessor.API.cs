using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    public void Play(AudioClip clip, float? volume = null, Transform track = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var borrowedSource, isTracking, isTracking ? default : _cachedTransform.position))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;

        borrowedSource.TrackedTransform = track;
        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform track = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var borrowedSource, isTracking, isTracking ? default : _cachedTransform.position))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;

        borrowedSource.TrackedTransform = track;
        borrowedSource.PlayOneShot(clip);
    }

    public void Play(AudioClip clip, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var borrowedSource, isTracking: false, position))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;

        borrowedSource.TrackedTransform = null;
        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var borrowedSource, isTracking: false, position))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;

        borrowedSource.TrackedTransform = null;
        borrowedSource.PlayOneShot(clip);
    }

    public bool TryBorrow(out IWyrmSource pooledAudioSource)
    {
        bool successful = TryReserve(out pooledAudioSource, isTracking: true);
        if (successful)
            pooledAudioSource.IsBorrowed = true;

        return successful;
    }

    bool TryReserve(out IWyrmSource pooledAudioSource, bool isTracking = true, Vector3 staticPosition = default)
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

        pooledAudioSource = _availableSources[--_availableCount];
        int newIndex = _activeCount;

        pooledAudioSource.ActiveIndex = newIndex;
        _activeSources[newIndex] = pooledAudioSource;

        SourceTransforms.Add(pooledAudioSource.CachedTransform);
        TrackedTransforms.Add(_cachedTransform);

        if (!isTracking)
        {
            IsTracking[newIndex] = 0;
            pooledAudioSource.CachedTransform.position = staticPosition;
            TrackedPositions[newIndex] = staticPosition;
            SourcePositions[newIndex] = staticPosition;
        }
        else
        {
            IsTracking[newIndex] = 1;
            SourcePositions[newIndex] = pooledAudioSource.CachedPosition;
            TrackedPositions[newIndex] = _cachedTransform.position;
        }

        SourceActiveStates[newIndex] = 1;
        SourceMinDistances[newIndex] = pooledAudioSource.minDistance;
        SourceMaxDistances[newIndex] = pooledAudioSource.maxDistance;
        OutputNormalizedRoomMixVolume[newIndex] = 0f;

        _activeCount++;
        return true;
    }


    internal void Return(IWyrmSource pooledAudioSource)
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
}