using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    [field: SerializeField]
    public AudioSource ASource { get; set; }

    public bool IsBorrowed { get; set; } = false;
    public int ActiveIndex { get; set; } = -1;
    public WyrmMixerGroupProcessor Manager { get; private set; }

    public Transform CachedTransform { get; private set; } = default;

    private Transform _trackedTransform;
    public Transform TrackedTransform
    {
        get => _trackedTransform;
        set
        {
            if (_trackedTransform == value) return;
            _trackedTransform = value;

            if (ActiveIndex >= 0)
                Manager.UpdateTrackedTransform(ActiveIndex, _trackedTransform);
        }
    }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }

    public WyrmMixerGroupConfig Config { get; private set; }

    public virtual void Initialize(WyrmMixerGroupProcessor manager)
    {
        Manager = manager;
        ASource.outputAudioMixerGroup = Manager.config.targetMixerGroup;
    }

    protected virtual void Awake()
    {
        CachedTransform = transform;
    }

    public virtual void Play(AudioClip clip, float? volume = null, Transform track = null)
    {
        if (volume.HasValue) this.volume = volume.Value;
        if (track != null) TrackedTransform = track;

        this.clip = clip;
        Play();
    }

    public virtual void Play(WyrmSoundBank bank, float? volume = null, Transform track = null)
    {
        if (volume.HasValue) this.volume = volume.Value;
        if (track != null) TrackedTransform = track;

        this.clip = bank.mainBodyClips[0];
        Play();
    }

    public virtual void Deactivate()
    {
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