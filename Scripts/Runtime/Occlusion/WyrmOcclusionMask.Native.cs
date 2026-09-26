using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmOcclusionMask
{
    protected override int AllocatedCapacity => HC.MAX_OCC_MASKS;

    const int AllocatedSampleCapacity = HC.MAX_OCC_MASKS * HC.MAX_OCC_SAMPLES_PER_MASK;

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

        SampleLocalPositions    = new(AllocatedSampleCapacity, Allocator.Persistent);
        SampleWeights           = new(AllocatedSampleCapacity, Allocator.Persistent);
        SampleIsDiscardable     = new(AllocatedSampleCapacity, Allocator.Persistent);
        SampleWorldPositions    = new(AllocatedSampleCapacity, Allocator.Persistent);
        SampleIsDiscarded       = new(AllocatedSampleCapacity, Allocator.Persistent);
        SampleIsOccluded        = new(AllocatedSampleCapacity, Allocator.Persistent);

        RaycastCommandBuffer    = new(AllocatedSampleCapacity, Allocator.Persistent);
        RaycastResultBuffer     = new(AllocatedSampleCapacity, Allocator.Persistent);
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

            int dstOffset                         = removedIndex * HC.MAX_OCC_SAMPLES_PER_MASK;
            int srcOffset                         = lastIndex * HC.MAX_OCC_SAMPLES_PER_MASK;
            int chunkLength                       = HC.MAX_OCC_SAMPLES_PER_MASK;

            NativeArray<float3>.Copy(SampleLocalPositions,  srcOffset, SampleLocalPositions,    dstOffset, chunkLength);
            NativeArray<float>.Copy(SampleWeights,          srcOffset, SampleWeights,           dstOffset, chunkLength);
            NativeArray<bool>.Copy(SampleIsDiscardable,     srcOffset, SampleIsDiscardable,     dstOffset, chunkLength);
            NativeArray<bool>.Copy(SampleIsDiscarded,       srcOffset, SampleIsDiscarded,       dstOffset, chunkLength);
            NativeArray<bool>.Copy(SampleIsOccluded,        srcOffset, SampleIsOccluded,        dstOffset, chunkLength);
        }
        MaskTransforms.RemoveAtSwapBack(removedIndex);

        RecalculateDenseOffsets(); // Update packing!
    }
}
