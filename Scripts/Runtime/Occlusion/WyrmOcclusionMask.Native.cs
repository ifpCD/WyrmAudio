using Unity.Collections;
using UnityEngine;

public partial class WyrmOcclusionMask
{
    protected override int AllocatedCapacity => 8000;

    public static NativeArray<float> TargetOcclusionValue01s;

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
