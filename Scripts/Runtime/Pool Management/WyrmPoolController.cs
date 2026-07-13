using System;
using System.Collections.Generic;
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

    // Source Inputs
    internal NativeArray<float> MinDistances;
    internal NativeArray<float> MaxDistances;

    internal NativeArray<byte> UseOcclusions;
    internal NativeArray<byte> UsePropagations;
    internal NativeArray<byte> UseReflections;

    internal NativeArray<byte> IsSourceTrackingTransform;

    internal NativeArray<float3> SourcePositions;
    internal NativeArray<float3> TrackedPositions;

    // Occlusion Input Buffers
    internal NativeArray<RaycastCommand> OcclusionCommands;
    internal NativeArray<RaycastHit> OcclusionHitResults;

    // Propagation Input Buffers
    internal NativeArray<int> SourceRoomIdentifiers;
    internal NativeArray<float3> PropagationDirections;
    internal NativeArray<float> PropagationDistances;

    // Output Buffers
    internal NativeArray<float> TargetOcclusion01s;
    internal NativeArray<float3> TargetPropagationEQ01;
    internal NativeArray<float> PropagationSHCoeffOutputs;

    internal NativeArray<IntPtr> Pointers;

    internal TransformAccessArray SourceTransforms;
    internal TransformAccessArray TrackedTransforms;

    // Lerp Output Values (these ones get passed to Phonon)
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

        ActiveSources = new IWyrmSource[MaximumCapacity];
        PlaybackEndTimes = new double[MaximumCapacity];

        SourceTransforms = new TransformAccessArray(MaximumCapacity);
        TrackedTransforms = new TransformAccessArray(MaximumCapacity);

        IsSourceTrackingTransform = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);
        SourceRoomIdentifiers = new NativeArray<int>(MaximumCapacity, Allocator.Persistent);
        SourcePositions = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);
        TrackedPositions = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);

        MinDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        MaxDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);

        UsePropagations = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);
        UseOcclusions = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);

        PropagationDirections = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);
        PropagationDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        TargetPropagationEQ01 = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);

        OcclusionCommands = new NativeArray<RaycastCommand>(MaximumCapacity, Allocator.Persistent);
        OcclusionHitResults = new NativeArray<RaycastHit>(MaximumCapacity, Allocator.Persistent);
        TargetOcclusion01s = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);

        Pointers = new NativeArray<IntPtr>(MaximumCapacity, Allocator.Persistent);
        PropagationSHCoeffOutputs = new NativeArray<float>(MaximumCapacity * 16, Allocator.Persistent); // 16 covers up to 3rd Order

        CurrentOcclusion01 = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        CurrentPropagationEQ01s = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);

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
        if (IsSourceTrackingTransform.IsCreated) IsSourceTrackingTransform.Dispose();
        if (SourceRoomIdentifiers.IsCreated) SourceRoomIdentifiers.Dispose();
        if (SourcePositions.IsCreated) SourcePositions.Dispose();
        if (TrackedPositions.IsCreated) TrackedPositions.Dispose();

        if (MinDistances.IsCreated) MinDistances.Dispose();
        if (MaxDistances.IsCreated) MaxDistances.Dispose();
        if (UsePropagations.IsCreated) UsePropagations.Dispose();
        if (UseOcclusions.IsCreated) UsePropagations.Dispose();
        if (PropagationDirections.IsCreated) PropagationDirections.Dispose();
        if (PropagationDistances.IsCreated) PropagationDistances.Dispose();
        if (TargetPropagationEQ01.IsCreated) TargetPropagationEQ01.Dispose();

        if (OcclusionCommands.IsCreated) OcclusionCommands.Dispose();
        if (OcclusionHitResults.IsCreated) OcclusionHitResults.Dispose();
        if (TargetOcclusion01s.IsCreated) TargetOcclusion01s.Dispose();

        if (Pointers.IsCreated) Pointers.Dispose();
        if (PropagationSHCoeffOutputs.IsCreated) PropagationSHCoeffOutputs.Dispose();

        if (CurrentOcclusion01.IsCreated) PropagationSHCoeffOutputs.Dispose();
        if (CurrentPropagationEQ01s.IsCreated) PropagationSHCoeffOutputs.Dispose();
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