using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

public partial class WyrmPoolController : MonoBehaviour
{
    internal static WyrmPoolController Instance { get; private set; }

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
    internal NativeArray<int> SourceRoomIdentifiers;
    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;
    internal NativeArray<byte> SourceActiveStates;
    internal NativeArray<float> SourceMinDistances;
    internal NativeArray<float> SourceMaxDistances;
    internal NativeArray<byte> SourceUsePropagation;

    public NativeArray<float3> PropagationDirections;
    public NativeArray<float> PropagationDistances;
    public NativeArray<float3> PropagationPathEQs;

    // Occlusion Buffers
    public NativeArray<RaycastCommand> OcclusionCommands;
    public NativeArray<RaycastHit> OcclusionHits;
    public NativeArray<float> SourceOcclusions;

    void Awake()
    {
        Instance = this;
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
        SourceRoomIdentifiers = new NativeArray<int>(totalMaxSize, Allocator.Persistent);
        SourcePositions = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        TrackedPositions = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        SourceActiveStates = new NativeArray<byte>(totalMaxSize, Allocator.Persistent);
        SourceMinDistances = new NativeArray<float>(totalMaxSize, Allocator.Persistent);
        SourceMaxDistances = new NativeArray<float>(totalMaxSize, Allocator.Persistent);
        SourceUsePropagation = new NativeArray<byte>(totalMaxSize, Allocator.Persistent);

        PropagationDirections = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);
        PropagationDistances = new NativeArray<float>(totalMaxSize, Allocator.Persistent);
        PropagationPathEQs = new NativeArray<float3>(totalMaxSize, Allocator.Persistent);

        OcclusionCommands = new NativeArray<RaycastCommand>(totalMaxSize, Allocator.Persistent);
        OcclusionHits = new NativeArray<RaycastHit>(totalMaxSize, Allocator.Persistent);
        SourceOcclusions = new NativeArray<float>(totalMaxSize, Allocator.Persistent);

        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            pools[config.targetMixerGroup] = new WyrmMixerPool(config, this);
        }
    }

    void OnDestroy()
    {
        pools.Clear();
        Instance = null;
        IsDisposed = true;

        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();
        if (IsTracking.IsCreated) IsTracking.Dispose();
        if (SourceRoomIdentifiers.IsCreated) SourceRoomIdentifiers.Dispose();
        if (SourcePositions.IsCreated) SourcePositions.Dispose();
        if (TrackedPositions.IsCreated) TrackedPositions.Dispose();
        if (SourceActiveStates.IsCreated) SourceActiveStates.Dispose();
        if (SourceMinDistances.IsCreated) SourceMinDistances.Dispose();
        if (SourceMaxDistances.IsCreated) SourceMaxDistances.Dispose();
        if (SourceUsePropagation.IsCreated) SourceUsePropagation.Dispose();
        if (PropagationDirections.IsCreated) PropagationDirections.Dispose();
        if (PropagationDistances.IsCreated) PropagationDistances.Dispose();
        if (PropagationPathEQs.IsCreated) PropagationPathEQs.Dispose();
        
        if (OcclusionCommands.IsCreated) OcclusionCommands.Dispose();
        if (OcclusionHits.IsCreated) OcclusionHits.Dispose();
        if (SourceOcclusions.IsCreated) SourceOcclusions.Dispose();
    }

    public static void UpdateTrackedTransform(int activeIndex, Transform track)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount) return;
        Instance.TrackedTransforms[activeIndex] = track != null ? track : Instance.CachedTransform;
    }

    public static void SetPlaybackEndTime(int activeIndex, double endTime)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount) return;
        Instance.PlaybackEndTimes[activeIndex] = endTime + 0.1;
    }

    public static double GetPlaybackEndTime(int activeIndex)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount) return -1;
        return Instance.PlaybackEndTimes[activeIndex];
    }
}