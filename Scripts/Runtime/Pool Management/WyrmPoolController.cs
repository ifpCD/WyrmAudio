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
    internal NativeArray<RaycastCommand> OcclusionCommands;
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

        InitializeBuffers();

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

        DisposeBuffers();
    }

    private void InitializeBuffers()
    {
        ActiveSources = new IWyrmSource[MaximumCapacity];
        PlaybackEndTimes = new double[MaximumCapacity];

        SourceTransforms = new TransformAccessArray(MaximumCapacity);
        TrackedTransforms = new TransformAccessArray(MaximumCapacity);

        IsTracking = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);
        SourceRoomIdentifiers = new NativeArray<int>(MaximumCapacity, Allocator.Persistent);
        SourcePositions = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);
        TrackedPositions = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);

        MinDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        MaxDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);

        UsePropagations = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);
        UseOcclusions = new NativeArray<byte>(MaximumCapacity, Allocator.Persistent);

        GraphDirections = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);
        GraphDistances = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        TargetPropagationEQ01 = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);

        OcclusionCommands = new NativeArray<RaycastCommand>(MaximumCapacity * 64, Allocator.Persistent);
        OcclusionHitResults = new NativeArray<RaycastHit>(MaximumCapacity * 64, Allocator.Persistent);
        TargetOcclusion01 = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);

        TargetTransmissionEQ01 = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Pointers = new NativeArray<IntPtr>(MaximumCapacity, Allocator.Persistent);
        TargetSHCoefficients = new NativeArray<float>(MaximumCapacity * 16, Allocator.Persistent); // 16 covers up to 3rd Order

        CurrentOcclusion01 = new NativeArray<float>(MaximumCapacity, Allocator.Persistent);
        CurrentPropagationEQ01s = new NativeArray<float3>(MaximumCapacity, Allocator.Persistent);
    }

    private void DisposeBuffers()
    {
        if (SourceTransforms.isCreated) SourceTransforms.Dispose();
        if (TrackedTransforms.isCreated) TrackedTransforms.Dispose();
        if (IsTracking.IsCreated) IsTracking.Dispose();
        if (SourceRoomIdentifiers.IsCreated) SourceRoomIdentifiers.Dispose();
        if (SourcePositions.IsCreated) SourcePositions.Dispose();
        if (TrackedPositions.IsCreated) TrackedPositions.Dispose();

        if (MinDistances.IsCreated) MinDistances.Dispose();
        if (MaxDistances.IsCreated) MaxDistances.Dispose();
        if (UsePropagations.IsCreated) UsePropagations.Dispose();
        if (UseOcclusions.IsCreated) UsePropagations.Dispose();
        if (GraphDirections.IsCreated) GraphDirections.Dispose();
        if (GraphDistances.IsCreated) GraphDistances.Dispose();
        if (TargetPropagationEQ01.IsCreated) TargetPropagationEQ01.Dispose();

        if (OcclusionCommands.IsCreated) OcclusionCommands.Dispose();
        if (OcclusionHitResults.IsCreated) OcclusionHitResults.Dispose();
        if (TargetOcclusion01.IsCreated) TargetOcclusion01.Dispose();

        if (Pointers.IsCreated) Pointers.Dispose();
        if (TargetSHCoefficients.IsCreated) TargetSHCoefficients.Dispose();

        if (TargetTransmissionEQ01.IsCreated) TargetOcclusion01.Dispose();

        if (CurrentOcclusion01.IsCreated) TargetSHCoefficients.Dispose();
        if (CurrentPropagationEQ01s.IsCreated) TargetSHCoefficients.Dispose();
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