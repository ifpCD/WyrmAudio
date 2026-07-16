using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-150)]
internal partial class GraphManager : MonoBehaviour
{
    internal static GraphManager Instance { get; private set; }

    private WyrmRoomShape[] roomShapes;
    private WyrmPortal[] portals;

    public NativeReference<int> ListenerRoomIdentifier;
    public NativeReference<float3> ListenerPosition { get; private set; }

    // Shapes are used both to figure out where listener/source position is
    // but also to generate and submit raycast samples
    // into the overarching room structure
    public NativeArray<float4x4> ShapeWorldToLocal;
    public NativeArray<float3> ShapeExtents;
    public NativeArray<int> ShapeRoomIdentifier;

    // Instead of using A*/Dijkstra for pathfinding
    // We raycast every sample between listener, and the source
    public NativeArray<int2> RoomSamplesIndices;
    public NativeArray<float3> RoomSamples;

    public NativeArray<float4x4> PortalWorldToLocal;
    public NativeArray<float3> PortalExtents;
    public NativeArray<int> PortalRoomA;
    public NativeArray<int> PortalRoomB;

    public NativeArray<float> PortalOpenness;

    private void Awake()
    {
        CollectGraphObjects();
        Allocate();

        InformChildrenGraphObjects();

        Instance = this;
    }

    void OnDestroy() => Deallocate();

    // csharpier-ignore
    void Allocate()
    {
        ListenerRoomIdentifier = new(allocator: Allocator.Persistent);
        ListenerPosition       = new(allocator: Allocator.Persistent);

        ShapeWorldToLocal      = new(roomShapes.Length, Allocator.Persistent);
        ShapeExtents           = new(roomShapes.Length, Allocator.Persistent);
        ShapeRoomIdentifier    = new(roomShapes.Length, Allocator.Persistent);

        RoomSamplesIndices     = new(roomShapes.Length, Allocator.Persistent);
        RoomSamples            = new(roomShapes.Length * 64, Allocator.Persistent);

        PortalWorldToLocal     = new(portals.Length, Allocator.Persistent);
        PortalExtents          = new(portals.Length, Allocator.Persistent);
        PortalRoomA            = new(portals.Length, Allocator.Persistent);
        PortalRoomB            = new(portals.Length, Allocator.Persistent);
        PortalOpenness         = new(portals.Length, Allocator.Persistent);
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

    private void CollectGraphObjects()
    {
        roomShapes = FindObjectsByType<WyrmRoomShape>(FindObjectsSortMode.None);
        portals = FindObjectsByType<WyrmPortal>(FindObjectsSortMode.None);
    }

    public void InformChildrenGraphObjects()
    {
        for (int i = 0; i < roomShapes.Length; i++)
            roomShapes[i].InformOfRegistration(this, i);

        for (int i = 0; i < portals.Length; i++)
            portals[i].InformOfRegistration(this, i);
    }
}
