using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
public class MeshAmbisonics : AmbiBase<MeshAmbisonics>
{
    protected override int AllocatedCapacity => 2000;

    public MeshFilter meshFilter;

    protected override void AllocateNative() { }

    protected override void DeallocateNative() { }

    protected override void LoadObjectToArrays() { }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex) { }
    }
}
