using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public partial class WyrmBaseSource : AmbiComponent<WyrmBaseSource>, IWyrmSource
{
    [HideInInspector]
    public AudioSource ASource { get; set; }

    public bool IsBorrowed { get; internal set; }
    internal WyrmMixerPool Pool { get; private set; }
    internal bool IsPooled => Pool != null;

    [field: SerializeField]
    public virtual bool UseReflections { get; set; } = false;

    [field: SerializeField]
    public virtual bool UseAmbisonics { get; set; } = false;

    [field: SerializeField]
    public virtual bool UseOcclusion { get; set; } = false;

    internal void Initialize(WyrmMixerPool pool)
    {
        Pool = pool;
        ASource.outputAudioMixerGroup = Pool.Config.targetMixerGroup;
    }

    // csharpier-ignore
    protected virtual void Awake()
    {
        CalculateVolumeAlpha();

        ASource         = this.EnsureReference(ASource);
        CachedTransform = transform;
        _clip           = ASource.clip;
        _loop           = ASource.loop;
        _volume         = ASource.volume;
        _pitch          = ASource.pitch;
        _minDistance    = ASource.minDistance;
        _maxDistance    = ASource.maxDistance;
    }

    protected virtual void OnValidate() { }

    public virtual void Play(AudioClip clip, Transform track = null, float? volume = null)
    {
        if (volume.HasValue)
        {
            TargetVolume = volume.Value;
            _currentVolume = volume.Value;
        }

        if (track != null)
            TrackedTransform = track;

        this.clip = clip;
        Play();
    }

    public virtual void Play(AbstractWyrmBank bank, Transform track = null, float? volume = null)
    {
        if (volume.HasValue)
        {
            TargetVolume = volume.Value;
            _currentVolume = volume.Value;
        }

        if (track != null)
            TrackedTransform = track;

        if (bank.PitchRandomization)
            ASource.pitch = 1f.WithVariation(bank.PitchDeviation);

        clip = bank.GetBodyClip();
        loop = bank.Loop;
        Play();
    }

    public void Return()
    {
        if (!IsPooled)
            throw new System.InvalidOperationException("Only pooled Wyrm sources can be returned.");

        Pool.ReturnToAvailable(this);
    }

    internal virtual bool Deactivate()
    {
        if (!IsRegistered)
            return false;

        Deregister();
        ASource.Stop();
        ResetState();
        return true;
    }

    internal void NotifyPlaybackCompletion() => Deregister();

    // csharpier-ignore
    public virtual void ResetState()
    {
        TrackedTransform = null;
        TargetVolume     = 1f;
        volume           = 1f;
        loop             = false;
        pitch            = 1f;
    }

    public bool isPlaying => ASource.isPlaying;

    public virtual void Play()
    {
        Register();

        if (loop)
        {
            SetPlaybackEndTime(AudioSettings.dspTime);
        }
        else
        {
            float activePitch = Mathf.Abs(ASource.pitch);
            float realDuration = activePitch > 0f ? ASource.clip.length / activePitch : float.MaxValue;
            SetPlaybackEndTime(AudioSettings.dspTime + realDuration);
        }

        ASource.Play();
    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        Register();

        double newEndTime = AudioSettings.dspTime + clip.length;

        if (newEndTime > PlaybackEndTime)
            SetPlaybackEndTime(newEndTime);

        ASource.PlayOneShot(clip);
    }

    public virtual void PlayOneShot(AbstractWyrmBank clip) => PlayOneShot(clip.GetBodyClip());

    public virtual void Stop()
    {
        SetPlaybackEndTime(double.NegativeInfinity);
        ASource.Stop();
    }

    protected virtual void OnDestroy() => Deregister();
}
