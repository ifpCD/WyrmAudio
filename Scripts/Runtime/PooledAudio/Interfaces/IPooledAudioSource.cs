using UnityEngine;

public interface IPooledAudioSource
{
    public void Play();
    public void SetClip(AudioClip clip);
    public void SetVolume(float volume);
}