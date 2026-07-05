using UnityEngine;

public class BasePooledSource : MonoBehaviour, IPooledAudioSource
{
    public AudioSource ASource;

    public bool IsPlaying => ASource.isPlaying;
    public bool IsOneShot { get; private set; } = default;

    public Transform BaseTransform { get; private set; } = default;
    public Transform TrackedTransform { get; set; } = default;

    protected virtual void Awake()
    {
        BaseTransform = gameObject.transform;
    }

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

    public virtual void PlayWyrmClip(WyrmAudioClip clip, float? playbackLengthOverride) { }

    public virtual void Deactivate()
    {
        TrackedTransform = null;
    }

    public virtual AudioClip clip
    {
        get => ASource.clip;
        set => ASource.clip = value;
    }

    public virtual float volume
    {
        get => ASource.volume;
        set => ASource.volume = value;
    }

    public virtual float pitch
    {
        get => ASource.pitch;
        set => ASource.pitch = value;
    }
}
