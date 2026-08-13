using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class OcclusionSample
{
    protected override int AllocatedCapacity => 8000 * WyrmOcclusionMask.MAX_SAMPLE_COUNT;

    public static NativeArray<int> MaskOwnerIndices;

    public static NativeArray<float3> LocalPositions;
    public static NativeArray<float> Weights;
    public static NativeArray<byte> IsOccluded;
    public static NativeArray<byte> IsDiscarded;

    protected override void AllocateNative()
    {
        throw new System.NotImplementedException();
    }

    protected override void DeallocateNative()
    {
        throw new System.NotImplementedException();
    }

    protected override void LoadObjectToArrays()
    {
        throw new System.NotImplementedException();
    }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        throw new System.NotImplementedException();
    }
}
