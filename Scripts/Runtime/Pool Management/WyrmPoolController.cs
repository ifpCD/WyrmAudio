using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public partial class WyrmPoolController : MonoBehaviour
{
    static readonly Dictionary<AudioMixerGroup, WyrmMixerPool> pools = new();

    internal Transform CachedTransform { get; private set; }
    internal bool IsDisposed { get; private set; }

    bool _nativeInitialized;

    void Awake()
    {
        CachedTransform = transform;
        WyrmAudioSettings settings = WyrmAudioSettings.Instance;
        int pooledCapacity = 0;

        foreach (WyrmMixerGroupConfig config in settings.ActiveMixerConfigs)
        {
            if (
                config == null
                || config.targetMixerGroup == null
                || config.WyrmAudioSourcePrefab == null
                || !config.WyrmAudioSourcePrefab.TryGetComponent<WyrmBaseSource>(out _)
            )
                throw new InvalidOperationException("Every active Wyrm mixer configuration must be fully assigned.");

            pooledCapacity = checked(pooledCapacity + config.maxSize);
        }

        if (pooledCapacity > settings.MaxActiveSources)
            throw new InvalidOperationException(
                $"Configured mixer pools require {pooledCapacity} sources but WyrmAudioSettings allows {settings.MaxActiveSources} active sources."
            );

        WyrmBaseSource.ConfigureCapacity(settings.MaxActiveSources);
        _nativeInitialized = true;

        foreach (WyrmMixerGroupConfig config in settings.ActiveMixerConfigs)
            pools.Add(config.targetMixerGroup, new WyrmMixerPool(config, this));
    }

    void OnDestroy()
    {
        IsDisposed = true;
        pools.Clear();

        if (_nativeInitialized)
            WyrmBaseSource.ShutdownNative();
    }
}
