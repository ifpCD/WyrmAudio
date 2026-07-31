using UnityEngine;

public partial class OcclusionSample
{
    protected override int MaximumCapacity => 8000*64;

    protected override void AllocateNative()
    {
        throw new System.NotImplementedException();
    }

    protected override void DeallocateNative()
    {
        throw new System.NotImplementedException();
    }

    protected override void LoadManagedToNative()
    {
        throw new System.NotImplementedException();
    }

    protected override void RemoveNativeAtSwapBack(int removedIndex, int lastIndex)
    {
        throw new System.NotImplementedException();
    }
}
