using UnityEngine;
using UnityEngine.Audio;

public interface IAudioPoolManager
{
    public void Play(AudioMixerGroup mixerGroup, AudioClip clip, float? volume = null, Transform trackedTransform = null);
    public BasePooledSource Borrow();
    public void Return(BasePooledSource pooledAudioSource);
}