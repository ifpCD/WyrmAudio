using UnityEngine;

public class BasePooledSource : MonoBehaviour, IPooledAudioSource
{
    public AudioSource ASource;

    public float Pitch => ASource.pitch;
    public bool IsPlaying => ASource.isPlaying;
    public bool IsOneShot { get; set; } = false;
    public Transform TrackedTransform { get; set; }

    public int PlayVersion { get; set; } = 0;

    public virtual void Play()
    {
        IsOneShot = true;
        ASource.Play();
    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        IsOneShot = false;
        ASource.PlayOneShot(clip);
    }

    public virtual void Stop() => ASource.Stop();

    public virtual void SetClip(AudioClip clip) => ASource.clip = clip;
    public virtual void SetVolume(float volume) => ASource.volume = volume;
    public virtual void SetPitch(float pitch) => ASource.pitch = pitch;
}
