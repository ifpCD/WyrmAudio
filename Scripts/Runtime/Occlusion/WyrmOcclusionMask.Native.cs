using Unity.Collections;
using UnityEngine;

public partial class WyrmOcclusionMask
{
    protected override int MaximumCapacity => 8000;

    public static NativeArray<byte> SampleCounts;

    public static NativeArray<float> TargetOcclusionValue01s;

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
