using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class OcclusionSample
{
    protected override int AllocatedCapacity => 8000 * WyrmOcclusionMask.MAX_SAMPLE_COUNT;

    public static NativeArray<float> InputWeights;
    public static NativeArray<byte> InputDiscardable;
    public static NativeArray<byte> InputLODGroup;

    public static NativeArray<int> MaskOwnerIndices;

    public static NativeArray<float3> LocalPositions;

    public static NativeArray<byte> IsOccluded;
    public static NativeArray<byte> IsDiscarded;

    public static NativeReference<int> DiscardRaycastCount;
    public static NativeReference<int> OcclusionRaycastCount;

    public static NativeArray<RaycastCommand> RaycastCommandBuffer;
    public static NativeArray<RaycastHit> RaycastResultBuffer;

    // csharpier-ignore
    protected override void AllocateNative()
    {

        InputWeights                         = new(AllocatedCapacity, Allocator.Persistent);
        InputDiscardable                     = new(AllocatedCapacity, Allocator.Persistent);

        MaskOwnerIndices                     = new(AllocatedCapacity, Allocator.Persistent);

        LocalPositions                       = new(AllocatedCapacity, Allocator.Persistent);

        IsOccluded                           = new(AllocatedCapacity, Allocator.Persistent);
        IsDiscarded                          = new(AllocatedCapacity, Allocator.Persistent);

        DiscardRaycastCount                  = new(Allocator.Persistent);
        OcclusionRaycastCount                = new(Allocator.Persistent);

        RaycastCommandBuffer                 = new(AllocatedCapacity, Allocator.Persistent);
        RaycastResultBuffer                  = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        InputWeights.TryDispose();
        InputDiscardable.TryDispose();

        MaskOwnerIndices.TryDispose();

        LocalPositions.TryDispose();

        IsOccluded.TryDispose();
        IsDiscarded.TryDispose();

        DiscardRaycastCount.TryDispose();
        OcclusionRaycastCount.TryDispose();

        RaycastCommandBuffer.TryDispose();
        RaycastResultBuffer.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        LocalPositions[SoAIndex]             = LocalPosition;
        InputWeights[SoAIndex]               = Weight;

        InputDiscardable[SoAIndex]           = Discardable.ToByte();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        InputWeights[removedIndex]           = InputWeights[lastIndex];
        InputDiscardable[removedIndex]       = InputDiscardable[lastIndex];

        MaskOwnerIndices[removedIndex]       = MaskOwnerIndices[lastIndex];
        LocalPositions[removedIndex]         = LocalPositions[lastIndex];
    }
}
