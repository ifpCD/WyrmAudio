using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public partial class WyrmPoolController : MonoBehaviour
{
    static readonly Dictionary<AudioMixerGroup, WyrmMixerPool> pools = new();

    internal Transform CachedTransform { get; private set; }
    internal bool IsDisposed { get; private set; }

    int _maximumCapacity;

    void Awake()
    {
        CachedTransform = transform;

        foreach (WyrmMixerGroupConfig config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            if (
                config == null
                || config.targetMixerGroup == null
                || config.WyrmAudioSourcePrefab == null
                || !config.WyrmAudioSourcePrefab.TryGetComponent<WyrmBaseSource>(out _)
            )
                throw new InvalidOperationException("Every active Wyrm mixer configuration must be fully assigned.");

            _maximumCapacity = checked(_maximumCapacity + config.maxSize);
        }

        if (_maximumCapacity == 0)
            return;

        WyrmBaseSource.ConfigureCapacity(_maximumCapacity);

        foreach (WyrmMixerGroupConfig config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
            pools.Add(config.targetMixerGroup, new WyrmMixerPool(config, this));
    }

    void OnDestroy()
    {
        IsDisposed = true;
        pools.Clear();

        if (_maximumCapacity != 0)
            WyrmBaseSource.ShutdownNative();
    }
}
