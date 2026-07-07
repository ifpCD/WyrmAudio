using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    public void Play(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;

        if (track != null)
        {
            borrowedSource.TrackedTransform = track;
            TrackedTransforms[_activeCount - 1] = track;
        }

        borrowedSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryBorrow(out var borrowedSource))
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

        // TODO: Cache this once per runtime
        var t = transform;

        SourceTransforms.Add(pooledAudioSource.CachedTransform);
        TrackedTransforms.Add(t);

        SourcePositions[newIndex] = pooledAudioSource.CachedPosition;
        TrackedPositions[newIndex] = t.position;
        SourceActiveStates[newIndex] = 1;
        SourceMinDistances[newIndex] = pooledAudioSource.minDistance;
        SourceMaxDistances[newIndex] = pooledAudioSource.maxDistance;
        OutputNormalizedRoomMixVolume[newIndex] = 0f;

        _activeCount++;

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
}