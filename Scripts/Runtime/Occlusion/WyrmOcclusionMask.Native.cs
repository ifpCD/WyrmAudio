using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmOcclusionMask
{
    protected override int AllocatedCapacity => HC.MAX_OCC_MASKS;

    public static NativeArray<float> TargetOcclusionValue01s;
    public static NativeArray<float4x4> LocalToWorlds;
    public static TransformAccessArray MaskTransforms;

    public static NativeArray<int> MaskSampleCounts;
    public static NativeArray<float3> SampleLocalPositions;
    public static NativeArray<float> SampleWeights;
    public static NativeArray<bool> SampleIsDiscardable;
    public static NativeArray<float3> SampleWorldPositions;
    public static NativeArray<bool> SampleIsDiscarded;
    public static NativeArray<bool> SampleIsOccluded; // purely for gizmos

    // dense packed via offsets
    public static NativeArray<int> MaskRaycastOffsets;
    public static NativeArray<RaycastCommand> RaycastCommandBuffer;
    public static NativeArray<RaycastHit> RaycastResultBuffer;

    public static int TotalActiveSamples { get; private set; }

    // csharpier-ignore
    protected override void AllocateNative()
    {
        TargetOcclusionValue01s = new(AllocatedCapacity, Allocator.Persistent);
        LocalToWorlds           = new(AllocatedCapacity, Allocator.Persistent);
        MaskTransforms          = new(AllocatedCapacity);
        MaskSampleCounts        = new(AllocatedCapacity, Allocator.Persistent);

        MaskRaycastOffsets      = new(AllocatedCapacity, Allocator.Persistent);

        SampleLocalPositions    = OcclusionSamples.Allocate<float3>(AllocatedCapacity, Allocator.Persistent);
        SampleWeights           = OcclusionSamples.Allocate<float>(AllocatedCapacity, Allocator.Persistent);
        SampleIsDiscardable     = OcclusionSamples.Allocate<bool>(AllocatedCapacity, Allocator.Persistent);
        SampleWorldPositions    = OcclusionSamples.Allocate<float3>(AllocatedCapacity, Allocator.Persistent);
        SampleIsDiscarded       = OcclusionSamples.Allocate<bool>(AllocatedCapacity, Allocator.Persistent);
        SampleIsOccluded        = OcclusionSamples.Allocate<bool>(AllocatedCapacity, Allocator.Persistent);

        // packed densely, but sized for every slot being full
        RaycastCommandBuffer    = OcclusionSamples.Allocate<RaycastCommand>(AllocatedCapacity, Allocator.Persistent);
        RaycastResultBuffer     = OcclusionSamples.Allocate<RaycastHit>(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        TargetOcclusionValue01s.TryDispose();
        LocalToWorlds.TryDispose();
        MaskTransforms.TryDispose();
        MaskSampleCounts.TryDispose();
        MaskRaycastOffsets.TryDispose();

        SampleLocalPositions.TryDispose();
        SampleWeights.TryDispose();
        SampleIsDiscardable.TryDispose();
        SampleWorldPositions.TryDispose();
        SampleIsDiscarded.TryDispose();
        SampleIsOccluded.TryDispose();

        RaycastCommandBuffer.TryDispose();
        RaycastResultBuffer.TryDispose();
    }

    private static void RecalculateDenseOffsets()
    {
        int offset = 0;
        for (int i = 0; i < ActiveCount; i++)
        {
            MaskRaycastOffsets[i] = offset;
            offset += MaskSampleCounts[i];
        }
        TotalActiveSamples = offset;
    }

    protected override void LoadObjectToArrays()
    {
        TargetOcclusionValue01s[SoAIndex] = 0f;
        LocalToWorlds[SoAIndex] = transform.localToWorldMatrix;
        MaskTransforms.Add(transform);
        SyncAllSamplesToNative();

        RecalculateDenseOffsets();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            TargetOcclusionValue01s[removedIndex] = TargetOcclusionValue01s[lastIndex];
            LocalToWorlds[removedIndex]           = LocalToWorlds[lastIndex];
            MaskSampleCounts[removedIndex]        = MaskSampleCounts[lastIndex];

            OcclusionSamples.Copy(SampleLocalPositions, lastIndex, SampleLocalPositions, removedIndex);
            OcclusionSamples.Copy(SampleWeights,        lastIndex, SampleWeights,        removedIndex);
            OcclusionSamples.Copy(SampleIsDiscardable,  lastIndex, SampleIsDiscardable,  removedIndex);
            OcclusionSamples.Copy(SampleIsDiscarded,    lastIndex, SampleIsDiscarded,    removedIndex);
            OcclusionSamples.Copy(SampleIsOccluded,     lastIndex, SampleIsOccluded,     removedIndex);
        }
        MaskTransforms.RemoveAtSwapBack(removedIndex);

        RecalculateDenseOffsets();
    }
}
