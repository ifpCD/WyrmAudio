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
        PortalWorldToLocal               = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
        PortalExtents                    = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
        PortalRoomA                      = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
        PortalRoomB                      = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
        PortalOpenness                   = new(length: AllocatedCapacity, allocator: Allocator.Persistent);
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

    protected override void LoadObjectToArrays()
    {
        SoASync();
    }

    // csharpier-ignore
    void SoASync()
    {
        if (!IsRegistered)
            return;

        PortalWorldToLocal[SoAIndex]  = WorldToLocal;
        PortalExtents[SoAIndex]       = Extents;
        PortalRoomA[SoAIndex]         = RoomA != null ? RoomA.RoomIdentifier : -1;
        PortalRoomB[SoAIndex]         = RoomB != null ? RoomB.RoomIdentifier : -1;
        PortalOpenness[SoAIndex]      = Openness;
    }
}
