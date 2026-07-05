using UnityEngine;

public class BasePooledSource : MonoBehaviour, IPooledAudioSource
{
    public AudioSource ASource;

    public float Pitch => ASource.pitch;
    public bool IsPlaying => ASource.isPlaying;
    public bool IsOneShot { get; private set; } = default;

    public Transform TrackedTransform { get; set; } = default;
    public int PlayVersion { get; set; } = default;

    public virtual void Play()
    {
        IsOneShot = false;
        ASource.Play();
    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        IsOneShot = true;
        ASource.PlayOneShot(clip);
    }

    public virtual void Stop() => ASource.Stop();

    public virtual void SetClip(AudioClip clip) => ASource.clip = clip;
    public virtual void SetVolume(float volume) => ASource.volume = volume;
    public virtual void SetPitch(float pitch) => ASource.pitch = pitch;
}
