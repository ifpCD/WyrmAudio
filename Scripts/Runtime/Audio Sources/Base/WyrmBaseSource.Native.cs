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
    internal static NativeArray<float3> SourcePositions;

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
    internal static NativeArray<RaycastCommand> OcclusionRayCommands;
    internal static NativeArray<RaycastHit> OcclusionHitResults;

    internal static NativeArray<int> SourceRoomIdentifiers;

    internal static NativeArray<float> TargetOcclusion01;
    internal static NativeArray<float3> TargetAmbisonicEQ01s;
    internal static NativeArray<float> TargetSHCoefficients;

    // Stateful
    internal static NativeArray<float> CurrentOcclusion01;
    internal static NativeArray<float3> CurrentTotalAmbisonicEQ01s;

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
        UseOcclusions              = new(AllocatedCapacity, Allocator.Persistent);

        SourceTransforms           = new(AllocatedCapacity);
        PositionTransforms         = new(AllocatedCapacity);
        SourcePositions            = new(AllocatedCapacity, Allocator.Persistent);

        InputVirtualPositions      = new(AllocatedCapacity, Allocator.Persistent);
        InputVerticalWidths        = new(AllocatedCapacity, Allocator.Persistent);
        InputHorizontalWidths      = new(AllocatedCapacity, Allocator.Persistent);
        InputDirectionalGains      = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbientGains          = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQHigh01s    = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQMid01s     = new(AllocatedCapacity, Allocator.Persistent);
        InputAmbisonicEQLow01s     = new(AllocatedCapacity, Allocator.Persistent);

        Pointers                   = new(AllocatedCapacity, Allocator.Persistent);

        OcclusionRayCommands       = new(AllocatedCapacity, Allocator.Persistent);
        OcclusionHitResults        = new(AllocatedCapacity, Allocator.Persistent);

        SourceRoomIdentifiers      = new(AllocatedCapacity, Allocator.Persistent);

        TargetOcclusion01          = new(AllocatedCapacity, Allocator.Persistent);
        TargetAmbisonicEQ01s       = new(AllocatedCapacity, Allocator.Persistent);
        TargetSHCoefficients       = new(AllocatedCapacity * 48, Allocator.Persistent);

        CurrentOcclusion01         = new(AllocatedCapacity, Allocator.Persistent);
        CurrentTotalAmbisonicEQ01s = new(AllocatedCapacity, Allocator.Persistent);

        // for(var i = 0; i < MaximumCapacity; i++)
        //     TargetSHCoefficients[i] = new(1f, 1f, 1f);
    }

    protected sealed override void DeallocateNative()
    {
        UseOcclusions.TryDispose();

        SourceTransforms.TryDispose();
        PositionTransforms.TryDispose();
        SourcePositions.TryDispose();

        InputVirtualPositions.TryDispose();
        InputVerticalWidths.TryDispose();
        InputHorizontalWidths.TryDispose();
        InputDirectionalGains.TryDispose();
        InputAmbientGains.TryDispose();
        InputAmbisonicEQHigh01s.TryDispose();
        InputAmbisonicEQMid01s.TryDispose();
        InputAmbisonicEQLow01s.TryDispose();

        Pointers.TryDispose();

        OcclusionRayCommands.TryDispose();
        OcclusionHitResults.TryDispose();

        SourceRoomIdentifiers.TryDispose();

        TargetOcclusion01.TryDispose();
        TargetAmbisonicEQ01s.TryDispose();
        TargetSHCoefficients.TryDispose();

        CurrentOcclusion01.TryDispose();
        CurrentTotalAmbisonicEQ01s.TryDispose();
    }

    // csharpier-ignore
    protected sealed override void LoadObjectToArrays()
    {
        int index = SoAIndex;

        SourceTransforms.Add(CachedTransform);
        PositionTransforms.Add(PositionTransform);

        UseOcclusions[index]            = UseOcclusion.ToByte();
        PlaybackEndTime                 = double.NegativeInfinity;

        InputVirtualPositions[index]    = VirtualPosition;
        InputVerticalWidths[index]      = VerticalWidth;
        InputHorizontalWidths[index]    = HorizontalWidth;
        InputDirectionalGains[index]    = DirectionalGain;
        InputAmbientGains[index]        = AmbientGain;
        InputAmbisonicEQHigh01s[index]  = AmbisonicEQHigh01;
        InputAmbisonicEQMid01s[index]   = AmbisonicEQMid01;
        InputAmbisonicEQLow01s[index]   = AmbisonicEQLow01;

        CurrentOcclusion01[index]       = 0f;
        CurrentTotalAmbisonicEQ01s[index]   = 1f;

        if (this is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            Pointers[index]             = phononSource.PhononSource.Get();
        else
            Pointers[index]             = IntPtr.Zero;
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
            CurrentTotalAmbisonicEQ01s[removedIndex]   = CurrentTotalAmbisonicEQ01s[lastIndex];
        }

        SourceTransforms.RemoveAtSwapBack(removedIndex);
        PositionTransforms.RemoveAtSwapBack(removedIndex);
    }
}
