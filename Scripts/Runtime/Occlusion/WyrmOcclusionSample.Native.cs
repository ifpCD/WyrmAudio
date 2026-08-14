using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class WyrmOcclusionSample
{
    protected override int AllocatedCapacity => 8000 * WyrmOcclusionMask.MAX_SAMPLE_COUNT;

    public static NativeArray<float> InputWeights;
    public static NativeArray<bool> InputDiscardable;
    public static NativeArray<bool> InputLODGroup;

    public static NativeArray<int> SampleToMask;

    public static NativeArray<float3> LocalPositions; // offset from the mask
    public static NativeArray<float3> WorldPositions; // calculated during discard raycast command generation passge

    public static NativeArray<bool> IsOccluded;
    public static NativeArray<bool> IsDiscarded;

    public static NativeArray<RaycastCommand> RaycastCommandBuffer;
    public static NativeArray<RaycastHit> RaycastResultBuffer;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        InputWeights                         = new(AllocatedCapacity, Allocator.Persistent);
        InputDiscardable                     = new(AllocatedCapacity, Allocator.Persistent);

        SampleToMask                         = new(AllocatedCapacity, Allocator.Persistent);

        LocalPositions                       = new(AllocatedCapacity, Allocator.Persistent);
        WorldPositions                       = new(AllocatedCapacity, Allocator.Persistent);

        IsOccluded                           = new(AllocatedCapacity, Allocator.Persistent);
        IsDiscarded                          = new(AllocatedCapacity, Allocator.Persistent);

        RaycastCommandBuffer                 = new(AllocatedCapacity, Allocator.Persistent);
        RaycastResultBuffer                  = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        InputWeights.TryDispose();
        InputDiscardable.TryDispose();

        SampleToMask.TryDispose();

        LocalPositions.TryDispose();
        WorldPositions.TryDispose();

        IsOccluded.TryDispose();
        IsDiscarded.TryDispose();

        RaycastCommandBuffer.TryDispose();
        RaycastResultBuffer.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        LocalPositions[SoAIndex]             = LocalPosition;
        InputWeights[SoAIndex]               = Weight;

        InputDiscardable[SoAIndex]           = Discardable;
        InputDiscardable[SoAIndex]           = Discardable;
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        InputWeights[removedIndex]           = InputWeights[lastIndex];
        InputDiscardable[removedIndex]       = InputDiscardable[lastIndex];

        SampleToMask[removedIndex]              = SampleToMask[lastIndex];

        LocalPositions[removedIndex]         = LocalPositions[lastIndex];
    }
}
