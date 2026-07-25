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

    Transform _trackedTransform;
    internal double PlaybackEndTime { get; private set; } = double.NegativeInfinity;

    internal static NativeArray<byte> UseOcclusions;
    internal static TransformAccessArray SourceTransforms;
    internal static TransformAccessArray PositionTransforms;
    internal static NativeArray<float3> SourcePositions;
    internal static NativeArray<IntPtr> Pointers;
    internal static NativeArray<RaycastCommand> OcclusionRayCommands;
    internal static NativeArray<RaycastHit> OcclusionHitResults;
    internal static NativeArray<int> SourceRoomIdentifiers;
    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetPropagationEQ01;
    internal static NativeArray<float> TargetSHCoefficients;
    internal static NativeArray<float> CurrentOcclusion01;
    internal static NativeArray<float3> CurrentPropagationEQ01;

    internal static WyrmBaseSource[] ActiveSources => EnabledInstances;

    public Transform CachedTransform { get; private set; }

    public Transform TrackedTransform
    {
        get => _trackedTransform;
        set
        {
            if (_trackedTransform == value)
                return;

            _trackedTransform = value;

            if (IsRegistered)
                PositionTransforms[NativeIndex] = PositionTransform;
        }
    }

    Transform PositionTransform => _trackedTransform != null ? _trackedTransform : CachedTransform;

    protected sealed override int MaximumCapacity => _maximumSourceCapacity;
    protected sealed override bool RetainNativeWhenEmpty => true;

    internal static void ConfigureCapacity(int maximumCapacity)
    {
        if (maximumCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCapacity));

        _maximumSourceCapacity = maximumCapacity;
    }

    internal static void ShutdownNative()
    {
        while (ActiveCount != 0)
            EnabledInstances[ActiveCount - 1].Deregister();

        DisposeRegistry();
        _maximumSourceCapacity = 0;
    }

    internal void Activate(Vector3 position, Transform trackedTransform)
    {
        CachedTransform.position = position;
        _trackedTransform = trackedTransform;
        Register();
    }

    internal void SetPlaybackEndTime(double endTime) => PlaybackEndTime = endTime + 0.1;

    // csharpier-ignore
    protected sealed override void AllocateNative()
    {
        UseOcclusions          = new(MaximumCapacity, Allocator.Persistent);
        SourceTransforms       = new(MaximumCapacity);
        PositionTransforms     = new(MaximumCapacity);
        SourcePositions        = new(MaximumCapacity, Allocator.Persistent);
        Pointers               = new(MaximumCapacity, Allocator.Persistent);
        OcclusionRayCommands   = new(MaximumCapacity, Allocator.Persistent);
        OcclusionHitResults    = new(MaximumCapacity, Allocator.Persistent);
        SourceRoomIdentifiers  = new(MaximumCapacity, Allocator.Persistent);
        TargetOcclusion01      = new(MaximumCapacity, Allocator.Persistent);
        TargetPropagationEQ01  = new(MaximumCapacity, Allocator.Persistent);
        TargetSHCoefficients   = new(MaximumCapacity * 16, Allocator.Persistent);
        CurrentOcclusion01     = new(MaximumCapacity, Allocator.Persistent);
        CurrentPropagationEQ01 = new(MaximumCapacity, Allocator.Persistent);
    }

    protected sealed override void DeallocateNative()
    {
        UseOcclusions.TryDispose();

        SourceTransforms.TryDispose();
        PositionTransforms.TryDispose();

        SourcePositions.TryDispose();
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

    protected sealed override void LoadManagedToNative()
    {
        int index = NativeIndex;

        SourceTransforms.Add(CachedTransform);
        PositionTransforms.Add(PositionTransform);

        SourcePositions[index]        = PositionTransform.position;

        UseOcclusions[index]          = UseOcclusion.ToByte();
        PlaybackEndTime               = double.NegativeInfinity;

        TargetOcclusion01[index]      = 0f;
        CurrentOcclusion01[index]     = 0f;
        TargetPropagationEQ01[index]  = 1f;
        CurrentPropagationEQ01[index] = 1f;

        if (this is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            Pointers[index] = phononSource.PhononSource.Get();
        else
            Pointers[index] = IntPtr.Zero;
    }

    // csharpier-ignore
    sealed protected override void RemoveNativeAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            UseOcclusions[removedIndex]          = UseOcclusions[lastIndex];
            SourcePositions[removedIndex]        = SourcePositions[lastIndex];
            Pointers[removedIndex]               = Pointers[lastIndex];
            SourceRoomIdentifiers[removedIndex]  = SourceRoomIdentifiers[lastIndex];
            TargetOcclusion01[removedIndex]      = TargetOcclusion01[lastIndex];
            TargetPropagationEQ01[removedIndex]  = TargetPropagationEQ01[lastIndex];
            CurrentOcclusion01[removedIndex]     = CurrentOcclusion01[lastIndex];
            CurrentPropagationEQ01[removedIndex] = CurrentPropagationEQ01[lastIndex];

            int removedShOffset                  = removedIndex * 16;
            int lastShOffset                     = lastIndex * 16;

            for (int coefficient = 0; coefficient < 16; coefficient++)
                TargetSHCoefficients[removedShOffset + coefficient] = TargetSHCoefficients[lastShOffset + coefficient];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        PositionTransforms.RemoveAtSwapBack(removedIndex);
    }
}
