using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

internal partial class GraphManager : MonoBehaviour
{
    internal NativeReference<int> ListenerRoomIdentifier;
    internal NativeReference<float3> ListenerPosition { get; private set; }

    // Shapes are used both to figure out where listener/source position is
    // but also to generate and submit raycast samples
    // into the overarching room structure
    internal NativeArray<float4x4> ShapeWorldToLocal;
    internal NativeArray<float3> ShapeExtents;
    internal NativeArray<int> ShapeRoomIdentifier;

    // Instead of using A*/Dijkstra for pathfinding
    // We raycast every sample between listener, and the source
    internal NativeArray<int2> RoomSamplesIndices;
    internal NativeArray<float3> RoomSamples;

    internal NativeArray<float4x4> PortalWorldToLocal;
    internal NativeArray<float3> PortalExtents;
    internal NativeArray<int> PortalRoomA;
    internal NativeArray<int> PortalRoomB;

    internal NativeArray<float> PortalOpenness;

    // csharpier-ignore
    void Allocate()
    {
        ListenerRoomIdentifier = new(allocator: Allocator.Persistent);
        ListenerPosition       = new(allocator: Allocator.Persistent);

        ShapeWorldToLocal      = new(_roomShapes.Length, Allocator.Persistent);
        ShapeExtents           = new(_roomShapes.Length, Allocator.Persistent);
        ShapeRoomIdentifier    = new(_roomShapes.Length, Allocator.Persistent);

        RoomSamplesIndices     = new(_roomShapes.Length, Allocator.Persistent);
        RoomSamples            = new(_roomShapes.Length * 64, Allocator.Persistent);

        PortalWorldToLocal     = new(_portals.Length, Allocator.Persistent);
        PortalExtents          = new(_portals.Length, Allocator.Persistent);
        PortalRoomA            = new(_portals.Length, Allocator.Persistent);
        PortalRoomB            = new(_portals.Length, Allocator.Persistent);
        PortalOpenness         = new(_portals.Length, Allocator.Persistent);
    }

    // csharpier-ignore
    void Deallocate()
    {
        if (ListenerRoomIdentifier.IsCreated)   ListenerRoomIdentifier.Dispose();
        if (ListenerPosition.IsCreated)         ListenerPosition.Dispose();

        if (ShapeExtents.IsCreated)             ShapeExtents.Dispose();
        if (ShapeWorldToLocal.IsCreated)        ShapeWorldToLocal.Dispose();
        if (ShapeRoomIdentifier.IsCreated)      ShapeRoomIdentifier.Dispose();

        if (RoomSamplesIndices.IsCreated)       RoomSamplesIndices.Dispose();
        if (RoomSamples.IsCreated)              RoomSamples.Dispose();

        if (PortalWorldToLocal.IsCreated)       PortalWorldToLocal.Dispose();
        if (PortalExtents.IsCreated)            PortalExtents.Dispose();
        if (PortalRoomA.IsCreated)              PortalRoomA.Dispose();
        if (PortalRoomB.IsCreated)              PortalRoomB.Dispose();
        if (PortalOpenness.IsCreated)           PortalOpenness.Dispose();
    }
}
