using System;
using Unity.Collections;
using Unity.Mathematics;

public sealed partial class WyrmRoomShape : AmbiMonoBehaviour<WyrmRoomShape>, IEasyCollider
{
    public static NativeArray<float4x4> ShapeWorldToLocal;
    public static NativeArray<float3> ShapeExtents;
    public static NativeArray<int> ShapeRoomIdentifier;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        ShapeWorldToLocal                        = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        ShapeExtents                             = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        ShapeRoomIdentifier                      = new(length: MaximumCapacity, allocator: Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        ShapeWorldToLocal.TryDispose();
        ShapeExtents.TryDispose();
        ShapeRoomIdentifier.TryDispose();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        ShapeWorldToLocal[removedIndex]          = ShapeWorldToLocal[lastIndex];
        ShapeExtents[removedIndex]               = ShapeExtents[lastIndex];
        ShapeRoomIdentifier[removedIndex]        = ShapeRoomIdentifier[lastIndex];
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        ShapeWorldToLocal[NativeIndex]           = WorldToLocal;
        ShapeExtents[NativeIndex]                = Extents;
        ShapeRoomIdentifier[NativeIndex]         = RoomIdentifier;
    }
}
