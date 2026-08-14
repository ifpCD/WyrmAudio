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

    internal static NativeArray<byte> UseOcclusions;

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

    internal static NativeArray<IntPtr> Pointers; // For Phonon

    // Output Scratch Buffers
    internal static NativeArray<RaycastCommand> RaycastCommandsBuffer;
    internal static NativeArray<RaycastHit> HitResultsBuffer;

    internal static NativeArray<int> SourceRoomIDs;

    internal static NativeArray<int> SourceToOcclusionMaskIndex;

    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetAmbisonicEQ01s;
    internal static NativeArray<float> TargetSH;

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

        Pointers                                       = new(AllocatedCapacity, Allocator.Persistent);

        RaycastCommandsBuffer                           = new(AllocatedCapacity, Allocator.Persistent);
        HitResultsBuffer                            = new(AllocatedCapacity, Allocator.Persistent);

        SourceRoomIDs                                  = new(AllocatedCapacity, Allocator.Persistent);

        TargetOcclusion01                              = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonicEQ01s                           = new(AllocatedCapacity, Allocator.Persistent);
        TargetSH                                       = new(AllocatedCapacity * 48, Allocator.Persistent);

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

        Pointers.TryDispose();

        RaycastCommandsBuffer.TryDispose();
        HitResultsBuffer.TryDispose();

        SourceRoomIDs.TryDispose();

        TargetOcclusion01.TryDispose();
        TargetAmbisonicEQ01s.TryDispose();
        TargetSH.TryDispose();

        CurrentOcclusion01.TryDispose();
        CurrentAmbisonicEQ01s.TryDispose();
    }

    // csharpier-ignore
    protected sealed override void LoadObjectToArrays()
    {
        SourceTransforms.Add(CachedTransform);
        PositionTransforms.Add(PositionTransform);

        UseOcclusions[SoAIndex]                           = UseOcclusion.ToByte();
        PlaybackEndTime                                   = double.NegativeInfinity;

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

        if (this is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            Pointers[SoAIndex]                            = phononSource.PhononSource.Get();
        else
            Pointers[SoAIndex]                            = IntPtr.Zero;
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

            Pointers[removedIndex]                     = Pointers[lastIndex];

            CurrentOcclusion01[removedIndex]           = CurrentOcclusion01[lastIndex];
            CurrentAmbisonicEQ01s[removedIndex]        = CurrentAmbisonicEQ01s[lastIndex];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        PositionTransforms.RemoveAtSwapBack(removedIndex);
    }
}
