using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct LocateSourcesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> SourcePositions;

    [ReadOnly] public NativeArray<float4x4> ShapeWorldToLocal;
    [ReadOnly] public NativeArray<float3> ShapeExtents;
    [ReadOnly] public NativeArray<int> ShapeRoomIdentifier;

    [ReadOnly] public NativeArray<float4x4> PortalWorldToLocal;
    [ReadOnly] public NativeArray<float3> PortalExtents;
    [ReadOnly] public NativeArray<int> PortalRoomA;
    [ReadOnly] public NativeArray<int> PortalRoomB;

    [WriteOnly] public NativeArray<int> SourceRoomIdentifiers;


    public void Execute(int index)
    {
        SourceRoomIdentifiers[index] = GraphMath.GetRoomId(
            SourcePositions[index],

            ShapeWorldToLocal,
            ShapeExtents,
            ShapeRoomIdentifier,

            PortalWorldToLocal,
            PortalExtents,
            PortalRoomA,
            PortalRoomB
        );
    }
}

[BurstCompile]
public struct LocateListenerJob : IJob
{
    [ReadOnly] public float3 ListenerPosition;

    [ReadOnly] public NativeArray<float4x4> ShapeWorldToLocal;
    [ReadOnly] public NativeArray<float3> ShapeExtents;
    [ReadOnly] public NativeArray<int> ShapeRoomIdentifier;

    [ReadOnly] public NativeArray<float4x4> PortalWorldToLocal;
    [ReadOnly] public NativeArray<float3> PortalExtents;
    [ReadOnly] public NativeArray<int> PortalRoomA;
    [ReadOnly] public NativeArray<int> PortalRoomB;

    [WriteOnly] public NativeReference<int> ListenerRoomIdentifier;


    public void Execute()
    {
        ListenerRoomIdentifier.Value = GraphMath.GetRoomId(
            ListenerPosition,

            ShapeWorldToLocal,
            ShapeExtents,
            ShapeRoomIdentifier,

            PortalWorldToLocal,
            PortalExtents,
            PortalRoomA,
            PortalRoomB
        );
    }
}