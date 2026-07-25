using System;
using UnityEngine;
using UnityEngine.Audio;

public partial class WyrmPoolController : MonoBehaviour
{
    readonly struct PoolEntry
    {
        internal readonly AudioMixerGroup MixerGroup;
        internal readonly WyrmMixerPool Pool;

        internal PoolEntry(AudioMixerGroup mixerGroup, WyrmMixerPool pool)
        {
            MixerGroup = mixerGroup;
            Pool = pool;
        }
    }

    static PoolEntry[] _pools = Array.Empty<PoolEntry>();

    internal Transform CachedTransform { get; private set; }
    internal bool IsDisposed { get; private set; }

    bool _nativeInitialized;

    void Awake()
    {
        CachedTransform = transform;
        WyrmAudioSettings settings = WyrmAudioSettings.Instance;
        int configCount = settings.ActiveMixerConfigs.Count;
        int pooledCapacity = 0;

        for (int i = 0; i < configCount; i++)
        {
            WyrmMixerGroupConfig config = settings.ActiveMixerConfigs[i];

            if (
                config == null
                || config.targetMixerGroup == null
                || config.WyrmAudioSourcePrefab == null
                || !config.WyrmAudioSourcePrefab.TryGetComponent<WyrmBaseSource>(out _)
            )
                throw new InvalidOperationException("Every active Wyrm mixer configuration must be fully assigned.");

            for (int previousIndex = 0; previousIndex < i; previousIndex++)
            {
                if (ReferenceEquals(settings.ActiveMixerConfigs[previousIndex].targetMixerGroup, config.targetMixerGroup))
                    throw new InvalidOperationException(
                        $"Mixer Group {config.targetMixerGroup.name} has more than one active Wyrm mixer configuration."
                    );
            }

            pooledCapacity = checked(pooledCapacity + config.maxSize);
        }

        if (pooledCapacity > settings.MaxActiveSources)
            throw new InvalidOperationException(
                $"Configured mixer pools require {pooledCapacity} sources but WyrmAudioSettings allows {settings.MaxActiveSources} active sources."
            );

        WyrmBaseSource.ConfigureCapacity(settings.MaxActiveSources);
        _nativeInitialized = true;

        _pools = new PoolEntry[configCount];
        for (int i = 0; i < configCount; i++)
        {
            WyrmMixerGroupConfig config = settings.ActiveMixerConfigs[i];
            _pools[i] = new PoolEntry(config.targetMixerGroup, new WyrmMixerPool(config, this));
        }
    }

    void OnDestroy()
    {
        IsDisposed = true;
        _pools = Array.Empty<PoolEntry>();

        if (_nativeInitialized)
            WyrmBaseSource.ShutdownNative();
    }

    static WyrmMixerPool GetPool(AudioMixerGroup mixerGroup)
    {
        for (int i = 0; i < _pools.Length; i++)
        {
            ref readonly PoolEntry entry = ref _pools[i];
            if (ReferenceEquals(entry.MixerGroup, mixerGroup))
                return entry.Pool;
        }

        throw new InvalidOperationException(
            $"Mixer Group {mixerGroup.name} does not have an active Wyrm mixer configuration."
        );
    }
}
