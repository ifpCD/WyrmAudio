using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[Flags]
public enum SourceSettings : byte
{
    None = 0,
    UseTracking = 1 << 0,
    UseOcclusion = 1 << 1,
    UsePropagation = 1 << 2,
    UseReflections = 1 << 3,
    AutoSave = 1 << 4,
    DarkMode = 1 << 5,
    IsActive = 1 << 6,
    ShowTutorial = 1 << 7,
}

public partial class WyrmBaseSource
{
    static int _maximumSourceCapacity;

    bool _activationIsTracking;
    Vector3 _activationPosition;
    Transform _activationTrackedTransform;

    internal static NativeArray<byte> UseOcclusions;
    internal static NativeArray<byte> IsTracking;
    internal static TransformAccessArray SourceTransforms;
    internal static TransformAccessArray TrackedTransforms;
    internal static NativeArray<float3> SourcePositions;
    internal static NativeArray<float3> TrackedPositions;
    internal static NativeArray<IntPtr> Pointers;
    internal static NativeArray<RaycastCommand> OcclusionRayCommands;
    internal static NativeArray<RaycastHit> OcclusionHitResults;
    internal static NativeArray<int> SourceRoomIdentifiers;
    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetPropagationEQ01;
    internal static NativeArray<float> TargetSHCoefficients;
    internal static NativeArray<float> CurrentOcclusion01;
    internal static NativeArray<float3> CurrentPropagationEQ01;
    internal static double[] PlaybackEndTimes;

    internal static WyrmBaseSource[] ActiveSources => EnabledInstances;

    protected override int MaximumCapacity => _maximumSourceCapacity;
    protected override bool RetainNativeWhenEmpty => true;

    internal static void ConfigureCapacity(int maximumCapacity)
    {
        if (maximumCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCapacity));

        _maximumSourceCapacity = maximumCapacity;
    }

    internal static void ShutdownNative()
    {
        while (EnabledInstanceCount != 0)
            EnabledInstances[EnabledInstanceCount - 1].Deactivate();

        DisposeRegistry();
        _maximumSourceCapacity = 0;
    }

    internal void Activate(bool isTracking, Vector3 staticPosition, Transform trackedTransform)
    {
        _activationIsTracking = isTracking;
        _activationPosition = staticPosition;
        _activationTrackedTransform = trackedTransform;
        _trackedTransform = trackedTransform;
        Register();
    }

    internal void SetPlaybackEndTime(double endTime)
    {
        if (IsRegistered)
            PlaybackEndTimes[NativeIndex] = endTime + 0.1;
    }

    internal double GetPlaybackEndTime() => IsRegistered ? PlaybackEndTimes[NativeIndex] : -1;

    protected override void AllocateNative()
    {
        int capacity = MaximumCapacity;

        PlaybackEndTimes = new double[capacity];
        UseOcclusions = new(capacity, Allocator.Persistent);
        IsTracking = new(capacity, Allocator.Persistent);
        SourceTransforms = new(capacity);
        TrackedTransforms = new(capacity);
        SourcePositions = new(capacity, Allocator.Persistent);
        TrackedPositions = new(capacity, Allocator.Persistent);
        Pointers = new(capacity, Allocator.Persistent);
        OcclusionRayCommands = new(capacity, Allocator.Persistent);
        OcclusionHitResults = new(capacity, Allocator.Persistent);
        SourceRoomIdentifiers = new(capacity, Allocator.Persistent);
        TargetOcclusion01 = new(capacity, Allocator.Persistent);
        TargetPropagationEQ01 = new(capacity, Allocator.Persistent);
        TargetSHCoefficients = new(capacity * 16, Allocator.Persistent);
        CurrentOcclusion01 = new(capacity, Allocator.Persistent);
        CurrentPropagationEQ01 = new(capacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        PlaybackEndTimes = null;
        UseOcclusions.TryDispose();
        IsTracking.TryDispose();

        SourceTransforms.TryDispose();
        TrackedTransforms.TryDispose();

        SourcePositions.TryDispose();
        TrackedPositions.TryDispose();
        Pointers.TryDispose();
        OcclusionRayCommands.TryDispose();
        OcclusionHitResults.TryDispose();
        SourceRoomIdentifiers.TryDispose();
        TargetOcclusion01.TryDispose();
        TargetPropagationEQ01.TryDispose();
        TargetSHCoefficients.TryDispose();
        CurrentOcclusion01.TryDispose();
        CurrentPropagationEQ01.TryDispose();
    }

    protected override void LoadManagedToNative()
    {
        int index = NativeIndex;

        SourceTransforms.Add(CachedTransform);
        TrackedTransforms.Add(_activationTrackedTransform != null ? _activationTrackedTransform : CachedTransform);

        if (_activationIsTracking)
        {
            IsTracking[index] = 1;
            float3 position = _activationTrackedTransform != null ? _activationTrackedTransform.position : CachedTransform.position;
            TrackedPositions[index] = position;
            SourcePositions[index] = position;
        }
        else
        {
            IsTracking[index] = 0;
            CachedTransform.position = _activationPosition;
            TrackedPositions[index] = _activationPosition;
            SourcePositions[index] = _activationPosition;
        }

        UseOcclusions[index] = UseOcclusion.ToByte();
        PlaybackEndTimes[index] = double.MaxValue;
        SourceRoomIdentifiers[index] = -1;
        TargetOcclusion01[index] = 0f;
        CurrentOcclusion01[index] = 0f;
        TargetPropagationEQ01[index] = 1f;
        CurrentPropagationEQ01[index] = 1f;

        if (this is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            Pointers[index] = phononSource.PhononSource.Get();
        else
            Pointers[index] = IntPtr.Zero;
    }

    protected override void RemoveNativeAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            PlaybackEndTimes[removedIndex] = PlaybackEndTimes[lastIndex];
            UseOcclusions[removedIndex] = UseOcclusions[lastIndex];
            IsTracking[removedIndex] = IsTracking[lastIndex];
            SourcePositions[removedIndex] = SourcePositions[lastIndex];
            TrackedPositions[removedIndex] = TrackedPositions[lastIndex];
            Pointers[removedIndex] = Pointers[lastIndex];
            SourceRoomIdentifiers[removedIndex] = SourceRoomIdentifiers[lastIndex];
            TargetOcclusion01[removedIndex] = TargetOcclusion01[lastIndex];
            TargetPropagationEQ01[removedIndex] = TargetPropagationEQ01[lastIndex];
            CurrentOcclusion01[removedIndex] = CurrentOcclusion01[lastIndex];
            CurrentPropagationEQ01[removedIndex] = CurrentPropagationEQ01[lastIndex];

            int removedShOffset = removedIndex * 16;
            int lastShOffset = lastIndex * 16;
            for (int coefficient = 0; coefficient < 16; coefficient++)
                TargetSHCoefficients[removedShOffset + coefficient] = TargetSHCoefficients[lastShOffset + coefficient];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        TrackedTransforms.RemoveAtSwapBack(removedIndex);
    }
}
