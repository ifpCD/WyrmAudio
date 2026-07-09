using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public partial class WyrmPoolController : MonoBehaviour
{
    private static WyrmPoolController instance;
    private static readonly Dictionary<AudioMixerGroup, WyrmMixerPool> pools = new();

    public Transform CachedTransform { get; private set; }
    public bool IsDisposed { get; private set; }

    private bool _hasFocus = true;
    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

    // Global contiguous Job states
    public IWyrmSource[] ActiveSources;
    public int ActiveCount;
    public double[] PlaybackEndTimes;

    internal TransformAccessArray SourceTransforms;
    internal TransformAccessArray TrackedTransforms;

    internal NativeArray<byte> IsTracking;
    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;
    internal NativeArray<float3> PropagationPositions;
    internal NativeArray<byte> SourceActiveStates;
    internal NativeArray<float> SourceMinDistances;
    internal NativeArray<float> SourceMaxDistances;
    internal NativeArray<float> OutputNormalizedRoomMixVolume;

    void Awake()
    {
        instance = this;
        CachedTransform = transform;

        int totalMaxSize = 0;
        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            totalMaxSize += config.maxSize;
        }

        ActiveSources = new IWyrmSource[totalMaxSize];
        PlaybackEndTimes = new double[totalMaxSize];

        SourceTransforms = new TransformAccessArray(totalMaxSize);
        TrackedTransforms = new TransformAccessArray(totalMaxSize);

        IsTracking = new NativeArray<byte>(totalMaxSize, Allocator.Persistent);
        SourcePositions = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        TrackedPositions = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        PropagationPositions = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        SourceActiveStates = new NativeArray<byte>(totalMaxSize, Allocator.Persistent);
        SourceMinDistances = new NativeArray<float>(totalMaxSize, Allocator.Persistent);
        SourceMaxDistances = new NativeArray<float>(totalMaxSize, Allocator.Persistent);
        OutputNormalizedRoomMixVolume = new NativeArray<float>(totalMaxSize, Allocator.Persistent);

        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            pools[config.targetMixerGroup] = new WyrmMixerPool(config, this);
        }
    }

    public static void Dispose()
    {
        if (instance != null) Destroy(instance.gameObject);
        pools.Clear();
    }

    void OnDestroy()
    {
        IsDisposed = true;
        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();
        if (IsTracking.IsCreated) IsTracking.Dispose();
        if (SourcePositions.IsCreated) SourcePositions.Dispose();
        if (TrackedPositions.IsCreated) TrackedPositions.Dispose();
        if (PropagationPositions.IsCreated) PropagationPositions.Dispose();
        if (SourceActiveStates.IsCreated) SourceActiveStates.Dispose();
        if (SourceMinDistances.IsCreated) SourceMinDistances.Dispose();
        if (SourceMaxDistances.IsCreated) SourceMaxDistances.Dispose();
        if (OutputNormalizedRoomMixVolume.IsCreated) OutputNormalizedRoomMixVolume.Dispose();
    }

    public static void UpdateTrackedTransform(int activeIndex, Transform track)
    {
        if (instance == null || activeIndex < 0 || activeIndex >= instance.ActiveCount) return;
        instance.TrackedTransforms[activeIndex] = track != null ? track : instance.CachedTransform;
    }

    public static void SetPlaybackEndTime(int activeIndex, double endTime)
    {
        if (instance == null || activeIndex < 0 || activeIndex >= instance.ActiveCount) return;
        instance.PlaybackEndTimes[activeIndex] = endTime;
    }

    public static double GetPlaybackEndTime(int activeIndex)
    {
        if (instance == null || activeIndex < 0 || activeIndex >= instance.ActiveCount) return -1;
        return instance.PlaybackEndTimes[activeIndex];
    }
}