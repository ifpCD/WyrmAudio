using System;
using SaintsField;
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
}

public partial class WyrmBaseSource
{
    static int _maximumSourceCapacity;

    internal double PlaybackEndTime { get; private set; } = double.NegativeInfinity;

    internal static NativeArray<bool> UseOcclusions;

    internal static TransformAccessArray SourceTransforms;
    internal static TransformAccessArray PositionTransforms;

    internal static NativeArray<float3> Positions;
    internal static NativeArray<quaternion> Rotations;
    internal static NativeArray<float4x4> LocalToWorlds;

    internal static NativeArray<AmbiHandle> OcclusionMaskHandles;
    internal static NativeArray<AmbiHandle> AmbisonicGeneratorHandles;

    internal static NativeArray<IntPtr> SpatializerPointers; // For Phonon

    // Scratch Buffers
    internal static NativeArray<int> SourceRoomIDs;

    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetAmbisonicEQ01s;
    internal static NativeArray<float> TargetAmbisonicOutputs;

    // Stateful Outputs
    internal static NativeArray<float> CurrentOcclusion01;
    internal static NativeArray<float3> CurrentAmbisonicEQ01s;

    protected sealed override bool RetainNativeWhenEmpty => true;

    internal static void Dispose()
    {
        DisposeRegistry();
        _maximumSourceCapacity = 0;
    }

    internal void Activate(Vector3 position, Transform trackedTransform)
    {
        CachedTransform.position = position;
        _trackedTransform = trackedTransform;

        if (UseOcclusion)
            OcclusionMask = this.EnsureReference(OcclusionMask);

        Register();
    }

    internal void SetPlaybackEndTime(double endTime) => PlaybackEndTime = endTime + HC.PLAYBACK_END_GRACE_TIME;

    // csharpier-ignore
    protected sealed override void AllocateNative()
    {
        UseOcclusions                                  = new(AllocatedCapacity, Allocator.Persistent);

        SourceTransforms                               = new(AllocatedCapacity);
        PositionTransforms                             = new(AllocatedCapacity);

        Positions                                      = new(AllocatedCapacity, Allocator.Persistent);
        Rotations                                      = new(AllocatedCapacity, Allocator.Persistent);
        LocalToWorlds                                  = new(AllocatedCapacity, Allocator.Persistent);

        SpatializerPointers                            = new(AllocatedCapacity, Allocator.Persistent);

        SourceRoomIDs                                  = new(AllocatedCapacity, Allocator.Persistent);

        OcclusionMaskHandles                           = new(AllocatedCapacity, Allocator.Persistent);
        AmbisonicGeneratorHandles                      = new(AllocatedCapacity, Allocator.Persistent);

        TargetOcclusion01                              = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonicEQ01s                           = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonicOutputs                         = new(AllocatedCapacity * HC.AMBISONIC_BUFFER_LENGTH, Allocator.Persistent);

        CurrentOcclusion01                             = new(AllocatedCapacity, Allocator.Persistent);
        CurrentAmbisonicEQ01s                          = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected sealed override void DeallocateNative()
    {
        UseOcclusions.TryDispose();

        SourceTransforms.TryDispose();
        PositionTransforms.TryDispose();

        Positions.TryDispose();
        Rotations.TryDispose();
        LocalToWorlds.TryDispose();

        SpatializerPointers.TryDispose();

        SourceRoomIDs.TryDispose();

        OcclusionMaskHandles.TryDispose();

        TargetOcclusion01.TryDispose();
        TargetAmbisonicEQ01s.TryDispose();
        TargetAmbisonicOutputs.TryDispose();

        CurrentOcclusion01.TryDispose();
        CurrentAmbisonicEQ01s.TryDispose();
    }

    protected sealed override void LoadObjectToArrays()
    {
        SoASync();
        SourceTransforms.Add(CachedTransform);
        PositionTransforms.Add(PositionTransform);
    }

    // csharpier-ignore
    void SoASync()
    {
        if (!IsRegistered)
            return;

        UseOcclusions[SoAIndex]                           = UseOcclusion;

        CurrentOcclusion01[SoAIndex]                      = 0f;
        CurrentAmbisonicEQ01s[SoAIndex]                   = 1f;

        OcclusionMaskHandles[SoAIndex]                    = OcclusionMaskHandle;
        AmbisonicGeneratorHandles[SoAIndex]                 = AmbisonicGenerator.Handle;

        if (this is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            SpatializerPointers[SoAIndex]                 = phononSource.PhononSource.Get();
        else
            SpatializerPointers[SoAIndex]                 = IntPtr.Zero;
    }

    // csharpier-ignore
    sealed protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            UseOcclusions[removedIndex]                = UseOcclusions[lastIndex];

            SpatializerPointers[removedIndex]          = SpatializerPointers[lastIndex];

            CurrentOcclusion01[removedIndex]           = CurrentOcclusion01[lastIndex];
            CurrentAmbisonicEQ01s[removedIndex]        = CurrentAmbisonicEQ01s[lastIndex];

            OcclusionMaskHandles[removedIndex] = OcclusionMaskHandles[lastIndex];
            AmbisonicGeneratorHandles[removedIndex] = AmbisonicGeneratorHandles[lastIndex];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        PositionTransforms.RemoveAtSwapBack(removedIndex);
    }
}
