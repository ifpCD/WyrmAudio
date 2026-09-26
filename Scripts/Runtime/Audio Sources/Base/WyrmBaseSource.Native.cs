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

    // Ambisonic Inputs
    internal static NativeArray<float3> InputVirtualPositions;
    internal static NativeArray<float> InputDirectionalGains;
    internal static NativeArray<float> InputAmbientGains;
    internal static NativeArray<float> InputHorizontalWidths;
    internal static NativeArray<float> InputVerticalWidths;
    internal static NativeArray<float> InputAmbisonicEQLow01s;
    internal static NativeArray<float> InputAmbisonicEQMid01s;
    internal static NativeArray<float> InputAmbisonicEQHigh01s;

    internal static NativeArray<IntPtr> SpatializerPointers; // For Phonon

    // Output Scratch Buffers
    internal static NativeArray<int> SourceRoomIDs;

    internal static NativeArray<AmbiHandle> OcclusionMaskHandles;

    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetAmbisonicEQ01s;
    internal static NativeArray<float> TargetAmbisonic;

    // Stateful
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

    const float PLAYBACK_END_GRACE_TIME = 0.1f;

    internal void SetPlaybackEndTime(double endTime) => PlaybackEndTime = endTime + PLAYBACK_END_GRACE_TIME;

    // csharpier-ignore
    protected sealed override void AllocateNative()
    {
        UseOcclusions                                  = new(AllocatedCapacity, Allocator.Persistent);

        SourceTransforms                               = new(AllocatedCapacity);
        PositionTransforms                             = new(AllocatedCapacity);

        Positions                                      = new(AllocatedCapacity, Allocator.Persistent);
        Rotations                                      = new(AllocatedCapacity, Allocator.Persistent);
        LocalToWorlds                                  = new(AllocatedCapacity, Allocator.Persistent);

        InputVirtualPositions                          = new(AllocatedCapacity, Allocator.Persistent);
        InputVerticalWidths                            = new(AllocatedCapacity, Allocator.Persistent);
        InputHorizontalWidths                          = new(AllocatedCapacity, Allocator.Persistent);
        InputDirectionalGains                          = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbientGains                              = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQHigh01s                        = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQMid01s                         = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQLow01s                         = new(AllocatedCapacity, Allocator.Persistent);

        SpatializerPointers                            = new(AllocatedCapacity, Allocator.Persistent);

        SourceRoomIDs                                  = new(AllocatedCapacity, Allocator.Persistent);

        OcclusionMaskHandles                             = new(AllocatedCapacity, Allocator.Persistent);

        TargetOcclusion01                              = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonicEQ01s                           = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonic                                = new(AllocatedCapacity * 48, Allocator.Persistent);

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

        InputVirtualPositions.TryDispose();
        InputVerticalWidths.TryDispose();
        InputHorizontalWidths.TryDispose();
        InputDirectionalGains.TryDispose();
        InputAmbientGains.TryDispose();
        InputAmbisonicEQHigh01s.TryDispose();
        InputAmbisonicEQMid01s.TryDispose();
        InputAmbisonicEQLow01s.TryDispose();

        SpatializerPointers.TryDispose();

        SourceRoomIDs.TryDispose();

        OcclusionMaskHandles.TryDispose();

        TargetOcclusion01.TryDispose();
        TargetAmbisonicEQ01s.TryDispose();
        TargetAmbisonic.TryDispose();

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

        InputVirtualPositions[SoAIndex]                   = VirtualPosition;
        InputVerticalWidths[SoAIndex]                     = VerticalWidth;
        InputHorizontalWidths[SoAIndex]                   = HorizontalWidth;
        InputDirectionalGains[SoAIndex]                   = DirectionalGain;
        InputAmbientGains[SoAIndex]                       = AmbientGain;
        InputAmbisonicEQHigh01s[SoAIndex]                 = AmbisonicEQHigh01;
        InputAmbisonicEQMid01s[SoAIndex]                  = AmbisonicEQMid01;
        InputAmbisonicEQLow01s[SoAIndex]                  = AmbisonicEQLow01;

        CurrentOcclusion01[SoAIndex]                      = 0f;
        CurrentAmbisonicEQ01s[SoAIndex]                   = 1f;

        OcclusionMaskHandles[SoAIndex]                    = OcclusionMaskHandle;

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

            InputVirtualPositions[removedIndex]        = InputVirtualPositions[lastIndex];
            InputVerticalWidths[removedIndex]          = InputVerticalWidths[lastIndex];
            InputHorizontalWidths[removedIndex]        = InputHorizontalWidths[lastIndex];
            InputDirectionalGains[removedIndex]        = InputDirectionalGains[lastIndex];
            InputAmbientGains[removedIndex]            = InputAmbientGains[lastIndex];
            InputAmbisonicEQHigh01s[removedIndex]      = InputAmbisonicEQHigh01s[lastIndex];
            InputAmbisonicEQMid01s[removedIndex]       = InputAmbisonicEQMid01s[lastIndex];
            InputAmbisonicEQLow01s[removedIndex]       = InputAmbisonicEQLow01s[lastIndex];

            SpatializerPointers[removedIndex]          = SpatializerPointers[lastIndex];

            CurrentOcclusion01[removedIndex]           = CurrentOcclusion01[lastIndex];
            CurrentAmbisonicEQ01s[removedIndex]        = CurrentAmbisonicEQ01s[lastIndex];

            OcclusionMaskHandles[removedIndex] = OcclusionMaskHandles[lastIndex];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        PositionTransforms.RemoveAtSwapBack(removedIndex);
    }
}
