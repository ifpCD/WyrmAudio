using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial class WyrmOcclusionMask
{
    protected override int AllocatedCapacity => 8000;

    public static NativeArray<float> TargetOcclusionValue01s;
    
    public static NativeArray<float4x4> LocalToWorlds;
    public static NativeArray<float3> WorldPositions;
    public static NativeArray<float3> LossyScales;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        TargetOcclusionValue01s = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        TargetOcclusionValue01s.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays() { }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex) { }
}
