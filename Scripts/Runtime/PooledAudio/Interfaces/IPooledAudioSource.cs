using UnityEngine;

public abstract class IPooledAudioSource : MonoBehaviour
{
    public abstract float Pitch { get; }
    public abstract bool IsPlaying { get; }
    public abstract Transform TrackedTransform { get; set; }
    public abstract bool IsOneShot { get; set; }

    public abstract int PlayVersion { get; set; }

    public abstract void Play();
    public abstract void PlayOneShot(AudioClip clip);
    public abstract void SetClip(AudioClip clip);
    public abstract void SetVolume(float volume);
    public abstract void SetPitch(float pitch);
    public abstract void Stop();
}