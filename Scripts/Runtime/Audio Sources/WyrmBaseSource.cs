using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    public AudioSource ASource;

    public int ActiveIndex { get; set; }
    public WyrmMixerGroupManager Manager { get; set; }

    public Transform CachedTransform { get; private set; } = default;
    
    private Transform _trackedTransform;
    public Transform TrackedTransform
    {
        get => _trackedTransform;
        set
        {
            _trackedTransform = value;
            if (Manager != null && isPlaying)
            {
                Manager.QueueTransformUpdate(this, value);
            }
        }
    }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }

    public WyrmMixerGroupConfig Config { get; private set; }

    public virtual void Initialize(WyrmMixerGroupConfig config)
    {
        Config = config;
        ASource.outputAudioMixerGroup = Config.targetMixerGroup;
    }

    protected virtual void Awake()
    {
        CachedTransform = transform;
    }

    public virtual void Play(WyrmSoundBank clip, float? volume = null, Transform trackedTransform = null)
    {
        if (volume.HasValue) this.volume = volume.Value;
        if (trackedTransform != null) TrackedTransform = trackedTransform;

        this.clip = clip.mainBodyClips[0]; // placeholder until I make DSP filters.
        Play();
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

    public bool isPlaying => ASource.isPlaying;

    public virtual bool loop
    {
        get => ASource.loop;
        set => ASource.loop = value;
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

    public virtual float minDistance
    {
        get => ASource.minDistance;
        set => ASource.minDistance = value;
    }

    public virtual float maxDistance
    {
        get => ASource.minDistance;
        set => ASource.minDistance = value;
    }

    public virtual void Play() => ASource.Play();

    public virtual void PlayOneShot(AudioClip clip) => ASource.PlayOneShot(clip);

    public virtual void Stop() => ASource.Stop();
}