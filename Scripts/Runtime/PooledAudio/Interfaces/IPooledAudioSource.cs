using UnityEngine;

public abstract class IPooledAudioSource : MonoBehaviour
{
    public abstract int PlayVersion { get; set; }
    public abstract void Play();
    public abstract void SetClip(AudioClip clip);
    public abstract void SetVolume(float volume);
}