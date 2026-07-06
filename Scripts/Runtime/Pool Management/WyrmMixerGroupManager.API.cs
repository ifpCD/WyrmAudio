using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupManager : MonoBehaviour
{
    public void Play(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.clip = clip;
        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (track != null) borrowedSource.TrackedTransform = track;

        borrowedSource.Play();
    }

    public void Play(WyrmSoundBank clip, float? volume = null, Transform track = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        borrowedSource.Play(clip, volume, track);
    }

    public void PlayOneShot(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (!TryBorrow(out var borrowedSource))
            return;

        if (volume.HasValue) borrowedSource.volume = volume.Value;
        if (track != null) borrowedSource.TrackedTransform = track;

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
}