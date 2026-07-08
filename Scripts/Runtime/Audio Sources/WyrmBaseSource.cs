using UnityEngine;

[DisallowMultipleComponent]
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

        if (track != null) TrackedTransform = track;

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

        if (track != null) TrackedTransform = track;
        if (bank.PitchRandomization) ASource.pitch = 1f.WithVariation(bank.PitchDeviation);

        clip = bank.GetBodyClip();
        Play();
    }

    public void Return() => Manager.Return(this);

    public virtual void Deactivate()
    {
        // ASource.Stop();
        _trackedTransform = null;

        // Reset tracking volumes on pool return
        TargetVolume = 1f;
        _currentVolume = 1f;

        loop = false;
        pitch = 1f;
        clip = null;
    }

    private AudioClip _clip;
    private bool _loop;
    private float _volume;
    private float _pitch;
    private float _minDistance;
    private float _maxDistance;

    public AudioClip clip
    {
        get => _clip;
        set
        {
            if (_clip == value) return;

            _clip = value;
            ASource.clip = value;
        }
    }

    public bool isPlaying => ASource.isPlaying;

    public bool loop
    {
        get => _loop;
        set
        {
            if (_loop == value) return;

            _loop = value;
            ASource.loop = value;
        }
    }

    public float volume
    {
        get => _volume;
        set
        {
            if (_volume == value) return;

            _volume = value;
            ASource.volume = value;
        }
    }

    public float pitch
    {
        get => _pitch;
        set
        {
            if (_pitch == value) return;

            _pitch = value;
            ASource.pitch = value;
        }
    }

    public float minDistance
    {
        get => _minDistance;
        set
        {
            if (_minDistance == value) return;

            _minDistance = value;
            ASource.minDistance = value;
        }
    }

    public float maxDistance
    {
        get => _maxDistance;
        set
        {
            if (_maxDistance == value) return;

            _maxDistance = value;
            ASource.maxDistance = value;
        }
    }

    public virtual void Play()
    {
        if (ASource.loop)
        {
            Manager.PlaybackEndTimes[ActiveIndex] = double.MaxValue;
        }
        else
        {
            float activePitch = Mathf.Abs(ASource.pitch);
            float realDuration = activePitch > 0f ? ASource.clip.length / activePitch : float.MaxValue;
            Manager.PlaybackEndTimes[ActiveIndex] = AudioSettings.dspTime + realDuration;
        }

        ASource.Play();
    }

    public virtual void PlayOneShot(AudioClip clip)
    {
        double newEndTime = AudioSettings.dspTime + clip.length;

        if (newEndTime > Manager.PlaybackEndTimes[ActiveIndex])
        {
            Manager.PlaybackEndTimes[ActiveIndex] = newEndTime;
        }

        ASource.PlayOneShot(clip);
    }

    public virtual void PlayOneShot(AbstractWyrmBank clip) => PlayOneShot(clip.GetBodyClip());

    public virtual void Stop()
    {
        Manager.PlaybackEndTimes[ActiveIndex] = -1;
        ASource.Stop();
    }
}