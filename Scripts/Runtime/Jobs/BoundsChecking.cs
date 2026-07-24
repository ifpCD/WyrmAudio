using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct LocateSourcesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float4x4> ShapeWorldToLocal;

    [ReadOnly]
    public NativeArray<float3> ShapeExtents;

    [ReadOnly]
    public NativeArray<int> ShapeRoomIdentifier;

    [ReadOnly]
    public NativeArray<float4x4> PortalWorldToLocal;

    [ReadOnly]
    public NativeArray<float3> PortalExtents;

    [ReadOnly]
    public NativeArray<int> PortalRoomA;

    [ReadOnly]
    public NativeArray<int> PortalRoomB;

    public int ShapeCount;
    public int PortalCount;

    [ReadOnly]
    public NativeArray<float3> SourcePositions;

    [WriteOnly]
    public NativeArray<int> SourceRoomIdentifiers;

    public void Execute(int index)
    {
        SourceRoomIdentifiers[index] = GraphMath.GetRoomId(
            SourcePositions[index],
            ShapeWorldToLocal,
            ShapeExtents,
            ShapeRoomIdentifier,
            ShapeCount,
            PortalWorldToLocal,
            PortalExtents,
            PortalRoomA,
            PortalRoomB,
            PortalCount
        );
    }
}

[BurstCompile]
public struct LocateListenerJob : IJob
{
    [ReadOnly]
    public NativeArray<float4x4> ShapeWorldToLocal;

    [ReadOnly]
    public NativeArray<float3> ShapeExtents;

    [ReadOnly]
    public NativeArray<int> ShapeRoomIdentifier;

    [ReadOnly]
    public NativeArray<float4x4> PortalWorldToLocal;

    [ReadOnly]
    public NativeArray<float3> PortalExtents;

    [ReadOnly]
    public NativeArray<int> PortalRoomA;

    [ReadOnly]
    public NativeArray<int> PortalRoomB;

    public int ShapeCount;
    public int PortalCount;

    [ReadOnly]
    public float3 ListenerPosition;

    [WriteOnly]
    public NativeReference<int> ListenerRoomIdentifier;

    public void Execute()
    {
        ListenerRoomIdentifier.Value = GraphMath.GetRoomId(
            ListenerPosition,
            ShapeWorldToLocal,
            ShapeExtents,
            ShapeRoomIdentifier,
            ShapeCount,
            PortalWorldToLocal,
            PortalExtents,
            PortalRoomA,
            PortalRoomB,
            PortalCount
        );
    }
}
