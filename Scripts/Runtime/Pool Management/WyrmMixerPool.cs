using UnityEngine;

public class WyrmMixerPool
{
    public WyrmMixerGroupConfig Config { get; private set; }

    private readonly WyrmBaseSource[] _availableSources;
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

        _availableSources = new WyrmBaseSource[config.maxSize];

        for (int i = 0; i < config.initialSize; i++)
            CreatePooledAudioSource();
    }

    private void CreatePooledAudioSource()
    {
        if (_totalCreated >= Config.maxSize)
            return;

        GameObject go = Object.Instantiate(Config.WyrmAudioSourcePrefab, _poolRoot);
        if (go.TryGetComponent(out WyrmBaseSource source))
        {
            source.Initialize(this);
            _availableSources[_availableCount++] = source;
            _totalCreated++;
        }
    }

    internal void ReturnToAvailable(IWyrmSource source)
    {
        if (source is not WyrmBaseSource pooledSource || pooledSource.Pool != this)
            throw new System.ArgumentException("The source does not belong to this mixer pool.", nameof(source));

        if (!_controller.IsDisposed && pooledSource.Deactivate())
        {
            pooledSource.IsBorrowed = false;
            _availableSources[_availableCount++] = pooledSource;
        }
    }

    private bool TryReserve(out WyrmBaseSource source, Vector3 position, Transform trackedTransform)
    {
        source = null;
        if (_controller.IsDisposed)
            return false;

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
        source.Activate(position, trackedTransform);
        return true;
    }

    public void Play(AbstractWyrmBank bank, Transform track = null, float? volume = null)
    {
        if (!TryReserve(out var source, track != null ? track.position : _controller.CachedTransform.position, track))
            return;

        source.Play(bank, track, volume);
    }

    public void Play(AudioClip clip, Transform track = null, float? volume = null)
    {
        if (!TryReserve(out var source, track != null ? track.position : _controller.CachedTransform.position, track))
            return;

        source.Play(clip, track, volume);
    }

    public void Play(AbstractWyrmBank bank, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var source, position, null))
            return;

        source.Play(bank, volume: volume);
    }

    public void Play(AudioClip clip, Vector3 position, float? volume = null)
    {
        if (!TryReserve(out var source, position, null))
            return;

        source.Play(clip, volume: volume);
    }

    public bool TryBorrow(out IWyrmSource source)
    {
        bool successful = TryReserve(out WyrmBaseSource reservedSource, _controller.CachedTransform.position, null);
        source = reservedSource;
        
        if (successful)
            reservedSource.IsBorrowed = true;

        return successful;
    }
}
