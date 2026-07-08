using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    public void Play(AbstractWyrmBank bank, Transform track = null, float? volume = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var borrowedSource, isTracking, isTracking ? default : _cachedTransform.position))
            return;

        borrowedSource.Play(bank, track, volume);
    }

    public void Play(AudioClip clip, Transform track = null, float? volume = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var borrowedSource, isTracking, isTracking ? default : _cachedTransform.position))
            return;

        borrowedSource.Play(clip, track, volume);
    }

    public void Play(AbstractWyrmBank bank, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var borrowedSource, isTracking: false, position))
            return;

        borrowedSource.clip = bank.GetBodyClip();
        if (volume.HasValue) borrowedSource.volume = volume.Value;

        borrowedSource.TrackedTransform = null;
        borrowedSource.Play();
    }

    public void Play(AudioClip clip, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var borrowedSource, isTracking: false, position))
            return;

        borrowedSource.clip = clip;
        borrowedSource.TrackedTransform = null;
        if (volume.HasValue) borrowedSource.TargetVolume = volume.Value;
        
        borrowedSource.Play();
    }

    public bool TryBorrow(out IWyrmSource pooledAudioSource)
    {
        bool successful = TryReserve(out pooledAudioSource, isTracking: true);
        if (successful) pooledAudioSource.IsBorrowed = true;

        return successful;
    }

    bool TryReserve(out IWyrmSource pooledAudioSource, bool isTracking = true, Vector3 staticPosition = default)
    {
        pooledAudioSource = null;
        if (IsDisposed) return false;

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