using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-150)]
internal partial class GraphManager : MonoBehaviour
{
    internal static GraphManager Instance { get; private set; }

    public NativeReference<int> ListenerRoomIdentifier;
    public float3 ListenerPosition { get; private set; }

    public NativeArray<float4x4> ShapeWorldToLocal;
    public NativeArray<float3> ShapeExtents;
    public NativeArray<int> ShapeRoomIdentifier;


    public NativeArray<float4x4> PortalWorldToLocal;
    public NativeArray<float3> PortalExtents;
    public NativeArray<int> PortalRoomA;
    public NativeArray<int> PortalRoomB;

    public NativeArray<float> PortalOpenness;

    private WyrmRoomShape[] roomShapes;
    private WyrmPortal[] portals;

    private void Awake()
    {
        CollectGraphObjects();
        Allocate();
        
        InformChildrenGraphObjects();

        Instance = this;
    }

    void OnDestroy() => Deallocate();

    void Allocate()
    {
        ListenerRoomIdentifier = new NativeReference<int>(allocator: Allocator.Persistent);

        ShapeWorldToLocal = new NativeArray<float4x4>(roomShapes.Length, Allocator.Persistent);
        ShapeExtents = new NativeArray<float3>(roomShapes.Length, Allocator.Persistent);
        ShapeRoomIdentifier = new NativeArray<int>(roomShapes.Length, Allocator.Persistent);

        // Rooms = new NativeArray<RoomData>(roomShapes.Length, Allocator.Persistent);

        PortalWorldToLocal = new(portals.Length, Allocator.Persistent);
        PortalExtents = new(portals.Length, Allocator.Persistent);
        PortalRoomA = new(portals.Length, Allocator.Persistent);
        PortalRoomB = new(portals.Length, Allocator.Persistent);
        PortalOpenness = new(portals.Length, Allocator.Persistent);
    }

    void Deallocate()
    {
        if (ListenerRoomIdentifier.IsCreated) ListenerRoomIdentifier.Dispose();

        if (ShapeExtents.IsCreated) ShapeExtents.Dispose();
        if (ShapeWorldToLocal.IsCreated) ShapeWorldToLocal.Dispose();
        if (ShapeRoomIdentifier.IsCreated) ShapeRoomIdentifier.Dispose();

        if (PortalWorldToLocal.IsCreated) PortalWorldToLocal.Dispose();
        if (PortalExtents.IsCreated) PortalExtents.Dispose();
        if (PortalRoomA.IsCreated) PortalRoomA.Dispose();
        if (PortalRoomB.IsCreated) PortalRoomB.Dispose();
        if (PortalOpenness.IsCreated) PortalOpenness.Dispose();
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