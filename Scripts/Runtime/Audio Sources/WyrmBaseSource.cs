using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    public AudioSource ASource;

    public bool IsPlaying => ASource.isPlaying;

    public Transform BaseTransform { get; private set; } = default;
    public Transform TrackedTransform { get; set; } = default;

    public WyrmMixerGroupConfig Config { get; private set; }

    public virtual void SetConfig(WyrmMixerGroupConfig config)
    {
        Config = config;
        ASource.outputAudioMixerGroup = Config.targetMixerGroup;
    }

    protected virtual void Awake()
    {
        BaseTransform = gameObject.transform;
    }

    public virtual void Play()
    {
        ASource.Play();
    }

    public virtual void Play(WyrmAudioClip clip, float? playbackLengthOverride)
    {

    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        ASource.PlayOneShot(clip);
    }

    public virtual void Deactivate()
    {
        TrackedTransform = null;
        ASource.Stop();
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