using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Collections;
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

    public int MaximumCapacity { get; private set; } = 0;

    private bool _hasFocus = true;

    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

    public int ActiveCount { get; private set; }
    internal IWyrmSource[] ActiveSources;
    internal double[] PlaybackEndTimes;

    // Stateful Inputs
    internal NativeArray<float> MinDistances;
    internal NativeArray<float> MaxDistances;

    internal NativeArray<byte> UseOcclusions;
    internal NativeArray<byte> UsePropagations;

    internal NativeArray<byte> IsTracking;

    internal TransformAccessArray SourceTransforms;
    internal TransformAccessArray TrackedTransforms;

    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;

    internal NativeArray<IntPtr> Pointers;

    // Stateless Occlusion Buffers
    internal NativeArray<RaycastCommand> OcclusionRayCommands;
    internal NativeArray<RaycastHit> OcclusionHitResults;

    // Stateless Propagation Buffers
    internal NativeArray<int> SourceRoomIdentifiers;
    internal NativeArray<float3> GraphDirections;
    internal NativeArray<float> GraphDistances;

    // Stateless Output Buffers
    internal NativeArray<float> TargetOcclusion01;
    internal NativeArray<float3> TargetPropagationEQ01;
    internal NativeArray<float> TargetSHCoefficients;
    internal NativeArray<float3> TargetTransmissionEQ01;

    // Stateful Output Values
    internal NativeArray<float> CurrentOcclusion01;
    internal NativeArray<float3> CurrentPropagationEQ01s;

    void Awake()
    {
        Instance = this;
        CachedTransform = transform;

        foreach (var config in WyrmAudioSettings.Instance.ActiveMixerConfigs)
        {
            MaximumCapacity += config.maxSize;
        }

        Allocate();

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

        Deallocate();
    }

    // csharpier-ignore
    private void Allocate()
    {
        ActiveSources           = new IWyrmSource[MaximumCapacity];
        PlaybackEndTimes        = new double[MaximumCapacity];

        MinDistances            = new (MaximumCapacity, Allocator.Persistent);
        MaxDistances            = new (MaximumCapacity, Allocator.Persistent);

        UseOcclusions           = new (MaximumCapacity, Allocator.Persistent);
        UsePropagations         = new (MaximumCapacity, Allocator.Persistent);

        IsTracking              = new (MaximumCapacity, Allocator.Persistent);

        SourceTransforms        = new (MaximumCapacity);
        TrackedTransforms       = new (MaximumCapacity);

        SourcePositions         = new (MaximumCapacity, Allocator.Persistent);
        TrackedPositions        = new (MaximumCapacity, Allocator.Persistent);

        Pointers                = new (MaximumCapacity, Allocator.Persistent);

        // Single Raycast for now
        OcclusionRayCommands    = new (MaximumCapacity, Allocator.Persistent);
        OcclusionHitResults     = new (MaximumCapacity, Allocator.Persistent);

        SourceRoomIdentifiers   = new (MaximumCapacity, Allocator.Persistent);
        GraphDirections         = new (MaximumCapacity, Allocator.Persistent);
        GraphDistances          = new (MaximumCapacity, Allocator.Persistent);

        TargetPropagationEQ01   = new (MaximumCapacity, Allocator.Persistent);
        TargetOcclusion01       = new (MaximumCapacity, Allocator.Persistent);
        TargetSHCoefficients    = new (MaximumCapacity * 16, Allocator.Persistent); // up to 3rd Order
        TargetTransmissionEQ01  = new (MaximumCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        CurrentOcclusion01      = new (MaximumCapacity, Allocator.Persistent);
        CurrentPropagationEQ01s = new (MaximumCapacity, Allocator.Persistent);
    }

    // csharpier-ignore
    private void Deallocate()
    {
        if (MinDistances.IsCreated)                 MinDistances.Dispose();
        if (MaxDistances.IsCreated)                 MaxDistances.Dispose();

        if (UseOcclusions.IsCreated)                UseOcclusions.Dispose();
        if (UsePropagations.IsCreated)              UsePropagations.Dispose();

        if (IsTracking.IsCreated)                   IsTracking.Dispose();

        if (SourceTransforms.isCreated)             SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated)            TrackedTransforms.Dispose();

        if (SourcePositions.IsCreated)              SourcePositions.Dispose();
        if (TrackedPositions.IsCreated)             TrackedPositions.Dispose();

        if (Pointers.IsCreated)                     Pointers.Dispose();

        if (OcclusionRayCommands.IsCreated)         OcclusionRayCommands.Dispose();
        if (OcclusionHitResults.IsCreated)          OcclusionHitResults.Dispose();

        if (SourceRoomIdentifiers.IsCreated)        SourceRoomIdentifiers.Dispose();
        if (GraphDirections.IsCreated)              GraphDirections.Dispose();
        if (GraphDistances.IsCreated)               GraphDistances.Dispose();

        if (TargetPropagationEQ01.IsCreated)        TargetPropagationEQ01.Dispose();
        if (TargetOcclusion01.IsCreated)            TargetOcclusion01.Dispose();
        if (TargetSHCoefficients.IsCreated)         TargetSHCoefficients.Dispose();
        if (TargetTransmissionEQ01.IsCreated)       TargetTransmissionEQ01.Dispose();

        if (CurrentOcclusion01.IsCreated)           CurrentOcclusion01.Dispose();
        if (CurrentPropagationEQ01s.IsCreated)      CurrentPropagationEQ01s.Dispose();
    }

    public static void UpdateTrackedTransform(int activeIndex, Transform track)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount)
            return;
        Instance.TrackedTransforms[activeIndex] = track != null ? track : Instance.CachedTransform;
    }

    public static void SetPlaybackEndTime(int activeIndex, double endTime)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount)
            return;
        Instance.PlaybackEndTimes[activeIndex] = endTime + 0.1;
    }

    public static double GetPlaybackEndTime(int activeIndex)
    {
        if (Instance == null || activeIndex < 0 || activeIndex >= Instance.ActiveCount)
            return -1;
        return Instance.PlaybackEndTimes[activeIndex];
    }
}
