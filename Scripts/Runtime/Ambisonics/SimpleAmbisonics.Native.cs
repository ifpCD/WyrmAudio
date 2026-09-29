using Unity.Collections;
using UnityEngine.Jobs;

internal sealed partial class SimpleAmbisonics
{
    internal static TransformAccessArray Transforms;
    internal static NativeArray<byte> Types;
    internal static NativeArray<AmbiHandle> GeneratorHandles;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        Transforms       = new(AllocatedCapacity);
        Types            = new(AllocatedCapacity, Allocator.Persistent);
        GeneratorHandles = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        Transforms.TryDispose();
        Types.TryDispose();
        GeneratorHandles.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        Transforms.Add(_generator.transform);
        Types[SoAIndex]            = (byte)_generator.SimpleType;
        GeneratorHandles[SoAIndex] = _generator.Handle;
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        Transforms.RemoveAtSwapBack(removedIndex);
        Types[removedIndex]            = Types[lastIndex];
        GeneratorHandles[removedIndex] = GeneratorHandles[lastIndex];
    }
}
