using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    public void Play(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryReserve(out var borrowedSource))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;

        if (track != null) borrowedSource.TrackedTransform = track;

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryReserve(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;

        if (track != null)
        {
            borrowedSource.TrackedTransform = track;
            TrackedTransforms[_activeCount - 1] = track;
        }
        borrowedSource.PlayOneShot(clip);
    }

    public bool TryBorrow(out IWyrmSource pooledAudioSource)
    {
        bool successful = TryReserve(out pooledAudioSource);
        if (successful)
            pooledAudioSource.IsBorrowed = true;

        return true;
    }

    bool TryReserve(out IWyrmSource pooledAudioSource)
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

        SourcePositions[newIndex] = pooledAudioSource.CachedPosition;
        TrackedPositions[newIndex] = _cachedTransform.position;
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