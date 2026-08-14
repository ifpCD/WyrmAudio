using System;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public partial class WyrmBaseSource : AmbiMonoBehaviour<WyrmBaseSource>, IWyrmSource
{
    [HideInInspector]
    internal AudioSource ASource { get; private set; }

    [Header("Debug")]
    public bool IsBorrowed { get; internal set; }

    internal WyrmMixerPool Pool { get; private set; }

    internal bool IsPooled => Pool != null;

    public AudioMixerGroup MixerGroup => ASource.outputAudioMixerGroup;

    [ShowInInspector]
    public virtual bool UseReflections { get; set; } = false;

    [ShowInInspector]
    public virtual bool UseAmbisonics { get; set; } = true;

    [ShowInInspector]
    public virtual bool UseOcclusion { get; set; } = true;

    internal void Initialize(WyrmMixerPool pool)
    {
        Pool = pool;
        ASource.outputAudioMixerGroup = Pool.Config.targetMixerGroup;
    }

    protected sealed override int AllocatedCapacity => _maximumSourceCapacity;

    internal static void ConfigureCapacity(int maximumCapacity)
    {
        if (maximumCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCapacity));

        _maximumSourceCapacity = maximumCapacity;
    }

    // csharpier-ignore
    protected virtual void Awake()
    {
        if (UseOcclusion)
            Mask        = this.EnsureReference(Mask);
            
        ASource         = this.EnsureReference(ASource);
        CachedTransform = transform;
        _clip           = ASource.clip;
        _loop           = ASource.loop;
        _volume         = ASource.volume;
        _pitch          = ASource.pitch;
        _minDistance    = ASource.minDistance;
        _maxDistance    = ASource.maxDistance;
    }

    protected virtual void OnValidate() => SoASync();

    public virtual void Play(AudioClip clip, Transform track = null, float? volume = null)
    {
        if (track != null)
            TrackedTransform = track;

        this.clip = clip;
        Play();
    }

    public virtual void Play(AbstractWyrmBank bank, Transform track = null, float? volume = null)
    {
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
            throw new InvalidOperationException("Only pooled Wyrm sources can be returned.");

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
