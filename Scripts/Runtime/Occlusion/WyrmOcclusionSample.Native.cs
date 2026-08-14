using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class WyrmOcclusionSample
{
    protected override int AllocatedCapacity => 8000 * WyrmOcclusionMask.MAX_SAMPLE_COUNT;

    public static NativeArray<float3> InputLocalPositions; // offset from the mask
    public static NativeArray<float> InputWeights;
    public static NativeArray<bool> InputDiscardable;
    public static NativeArray<byte> InputLODGroup;

    public static NativeArray<int> SampleToMask;

    public static NativeArray<float3> WorldPositions; // calculated during discard raycast command generation passge

    public static NativeArray<bool> IsOccluded;
    public static NativeArray<bool> IsDiscarded;

    public static NativeArray<RaycastCommand> RaycastCommandBuffer;
    public static NativeArray<RaycastHit> RaycastResultBuffer;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        InputLocalPositions                  = new(AllocatedCapacity, Allocator.Persistent);
        InputWeights                         = new(AllocatedCapacity, Allocator.Persistent);
        InputDiscardable                     = new(AllocatedCapacity, Allocator.Persistent);
        InputLODGroup                        = new(AllocatedCapacity, Allocator.Persistent);

        SampleToMask                         = new(AllocatedCapacity, Allocator.Persistent);

        WorldPositions                       = new(AllocatedCapacity, Allocator.Persistent);

        IsOccluded                           = new(AllocatedCapacity, Allocator.Persistent);
        IsDiscarded                          = new(AllocatedCapacity, Allocator.Persistent);

        RaycastCommandBuffer                 = new(AllocatedCapacity, Allocator.Persistent);
        RaycastResultBuffer                  = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        InputLocalPositions.TryDispose();
        InputWeights.TryDispose();
        InputDiscardable.TryDispose();
        InputLODGroup.TryDispose();

        SampleToMask.TryDispose();

        WorldPositions.TryDispose();

        IsOccluded.TryDispose();
        IsDiscarded.TryDispose();

        RaycastCommandBuffer.TryDispose();
        RaycastResultBuffer.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        InputLocalPositions[SoAIndex]        = LocalPosition;
        InputWeights[SoAIndex]               = Weight;
        InputLODGroup[SoAIndex]              = LODGroup;
        InputDiscardable[SoAIndex]           = Discardable;
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        InputLocalPositions[removedIndex]    = InputLocalPositions[lastIndex];
        InputWeights[removedIndex]           = InputWeights[lastIndex];
        InputLODGroup[removedIndex]          = InputLODGroup[lastIndex];
        InputDiscardable[removedIndex]       = InputDiscardable[lastIndex];

        SampleToMask[removedIndex]           = SampleToMask[lastIndex];
    }
}
