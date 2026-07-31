using System;
using Unity.Collections;
using Unity.Mathematics;

public sealed partial class WyrmPortal : AmbiMonoBehaviour<WyrmPortal>, IEasyCollider
{
    public static NativeArray<float4x4> PortalWorldToLocal;
    public static NativeArray<float3> PortalExtents;
    public static NativeArray<int> PortalRoomA;
    public static NativeArray<int> PortalRoomB;
    public static NativeArray<float3> PortalOpenness;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        PortalWorldToLocal               = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        PortalExtents                    = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        PortalRoomA                      = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        PortalRoomB                      = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        PortalOpenness                   = new(length: MaximumCapacity, allocator: Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        PortalWorldToLocal.TryDispose();
        PortalExtents.TryDispose();
        PortalRoomA.TryDispose();
        PortalRoomB.TryDispose();
        PortalOpenness.TryDispose();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        PortalWorldToLocal[removedIndex] = PortalWorldToLocal[lastIndex];
        PortalExtents[removedIndex]      = PortalExtents[lastIndex];
        PortalRoomA[removedIndex]        = PortalRoomA[lastIndex];
        PortalRoomB[removedIndex]        = PortalRoomB[lastIndex];
        PortalOpenness[removedIndex]     = PortalOpenness[lastIndex];
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        if (!IsRegistered)
            return;

        PortalWorldToLocal[NativeIndex]  = WorldToLocal;
        PortalExtents[NativeIndex]       = Extents;
        PortalRoomA[NativeIndex]         = RoomA != null ? RoomA.RoomIdentifier : -1;
        PortalRoomB[NativeIndex]         = RoomB != null ? RoomB.RoomIdentifier : -1;
        PortalOpenness[NativeIndex]      = Openness;
    }
}
