using UnityEngine;

[DisallowMultipleComponent]
public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
{
    [field: SerializeField]
    public AudioSource ASource { get; set; }

    public bool IsBorrowed { get; set; } = false;
    public int ActiveIndex { get; set; } = -1;
    public WyrmMixerPool Pool { get; private set; }

    [field: SerializeField]
    public bool UseReflections { get; set; } = false;

    [field: SerializeField]
    public bool UsePropagation { get; set; } = false;

    [field: SerializeField]
    public bool UseOcclusion { get; set; } = false;

    public WyrmMixerGroupConfig Config { get; private set; }

    public virtual void Initialize(WyrmMixerPool pool)
    {
        Pool = pool;
        ASource.outputAudioMixerGroup = Pool.Config.targetMixerGroup;
    }

    protected virtual void Awake()
    {
        CachedTransform = transform;
        CalculateVolumeAlpha();
        _clip = ASource.clip;
        _loop = ASource.loop;
        _volume = ASource.volume;
        _pitch = ASource.pitch;
        _minDistance = ASource.minDistance;
        _maxDistance = ASource.maxDistance;
    }

    private void OnValidate()
    {
        volumeTransitionTime = Mathf.Max(0f, volumeTransitionTime);
    }

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
        Play();
    }

    public void Return() => Pool.ReturnToAvailable(this);

    public virtual void Deactivate()
    {
        ResetState();
    }

    public virtual void ResetState()
    {
        TrackedTransform = null;
        TargetVolume = 1f;
        volume = 1f;
        loop = false;
        pitch = 1f;
    }

    public bool isPlaying => ASource.isPlaying;

    public virtual void Play()
    {
        if (ASource.loop)
        {
            WyrmPoolController.SetPlaybackEndTime(ActiveIndex, double.MaxValue);
        }
        else
        {
            float activePitch = Mathf.Abs(ASource.pitch);
            float realDuration = activePitch > 0f ? ASource.clip.length / activePitch : float.MaxValue;
            WyrmPoolController.SetPlaybackEndTime(ActiveIndex, AudioSettings.dspTime + realDuration);
        }

        ASource.Play();
    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        double newEndTime = AudioSettings.dspTime + clip.length;

        if (newEndTime > WyrmPoolController.GetPlaybackEndTime(ActiveIndex))
        {
            WyrmPoolController.SetPlaybackEndTime(ActiveIndex, newEndTime);
        }

        ASource.PlayOneShot(clip);
    }

    public virtual void PlayOneShot(AbstractWyrmBank clip) => PlayOneShot(clip.GetBodyClip());

    public virtual void Stop()
    {
        WyrmPoolController.SetPlaybackEndTime(ActiveIndex, -1);
        ASource.Stop();
    }
}
