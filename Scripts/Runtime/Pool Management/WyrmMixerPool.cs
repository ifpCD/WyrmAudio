using UnityEngine;

public class WyrmMixerPool
{
    public WyrmMixerGroupConfig Config { get; private set; }
    
    private readonly IWyrmSource[] _availableSources;
    private int _availableCount;
    private int _totalCreated;
    
    private readonly WyrmPoolController _controller;
    private readonly Transform _poolRoot;

    public WyrmMixerPool(WyrmMixerGroupConfig config, WyrmPoolController controller)
    {
        Config = config;
        _controller = controller;

        GameObject rootGo = new($"{config.targetMixerGroup.audioMixer.name} - {config.targetMixerGroup.name}");
        _poolRoot = rootGo.transform;
        _poolRoot.SetParent(_controller.transform);

        _availableSources = new IWyrmSource[config.maxSize];
        
        for (int i = 0; i < config.initialSize; i++)
            CreatePooledAudioSource();
    }

    private void CreatePooledAudioSource()
    {
        if (_totalCreated >= Config.maxSize) return;

        GameObject go = Object.Instantiate(Config.WyrmAudioSourcePrefab, _poolRoot);
        if (go.TryGetComponent(out IWyrmSource source))
        {
            source.Initialize(this);
            _availableSources[_availableCount++] = source;
            _totalCreated++;
        }
    }

    internal void ReturnToAvailable(IWyrmSource source)
    {
        _controller.ReturnSource(source);
        _availableSources[_availableCount++] = source;
    }

    private bool TryReserve(out IWyrmSource source, bool isTracking, Vector3 staticPosition, Transform trackTransform)
    {
        source = null;
        if (_controller.IsDisposed) return false;

        if (_availableCount == 0)
        {
            if (_totalCreated >= Config.maxSize)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WyrmAudio: Mixer Group {Config.targetMixerGroup.name} is overflowing, skipping playback.");
#endif
                return false;
            }
            CreatePooledAudioSource();
        }

        source = _availableSources[--_availableCount];
        _controller.ActivateSource(source, isTracking, staticPosition, trackTransform);
        return true;
    }

    public void Play(AbstractWyrmBank bank, Transform track = null, float? volume = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var source, isTracking, isTracking ? default : _controller.CachedTransform.position, track)) return;
        source.Play(bank, track, volume);
    }

    public void Play(AudioClip clip, Transform track = null, float? volume = null)
    {
        bool isTracking = track != null;
        if (!TryReserve(out var source, isTracking, isTracking ? default : _controller.CachedTransform.position, track)) return;
        source.Play(clip, track, volume);
    }

    public void Play(AbstractWyrmBank bank, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var source, false, position, null)) return;
        source.clip = bank.GetBodyClip();
        if (volume.HasValue) source.volume = volume.Value;
        source.Play();
    }

    public void Play(AudioClip clip, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var source, false, position, null)) return;
        source.clip = clip;
        if (volume.HasValue) source.TargetVolume = volume.Value;
        source.Play();
    }

    public bool TryBorrow(out IWyrmSource source)
    {
        bool successful = TryReserve(out source, true, default, null);
        if (successful) source.IsBorrowed = true;
        return successful;
    }
}