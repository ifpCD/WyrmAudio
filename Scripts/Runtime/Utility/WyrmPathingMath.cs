using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

internal static class GraphMath
{
    [BurstCompile]
    public static bool Contains(float3 worldPoint, float4x4 worldToLocal, float3 extents)
    {
        float3 local = math.transform(worldToLocal, worldPoint);

        return math.all(math.abs(local) <= extents);
    }

    [BurstCompile]
    public static int GetPortalRoom(float3 worldPoint, float4x4 worldToLocal, int roomA, int roomB)
    {
        float3 local = math.transform(worldToLocal, worldPoint);

        return local.z >= 0 ? roomB : roomA;
    }

    [BurstCompile]
    public static int GetPortalIndex(float3 worldPoint, in NativeArray<float4x4> portalWorldToLocal, in NativeArray<float3> portalExtents)
    {
        for (int portalIndex = 0; portalIndex < portalWorldToLocal.Length; portalIndex++)
        {
            if (Contains(worldPoint, portalWorldToLocal[portalIndex], portalExtents[portalIndex]))
                return portalIndex;
        }

        return -1;
    }

    [BurstCompile]
    public static int GetRoomId(
        float3 worldPoint,
        in NativeArray<float4x4> ShapeWorldToLocal,
        in NativeArray<float3> ShapeExtents,
        in NativeArray<int> ShapeRoomIdentifier,
        in NativeArray<float4x4> PortalWorldToLocal,
        in NativeArray<float3> PortalExtents,
        in NativeArray<int> PortalRoomA,
        in NativeArray<int> PortalRoomB
    )
    {
        for (int i = 0; i < ShapeWorldToLocal.Length; i++)
        {
            if (Contains(worldPoint, ShapeWorldToLocal[i], ShapeExtents[i]))
            {
                return ShapeRoomIdentifier[i];
            }
        }

        for (int i = 0; i < PortalWorldToLocal.Length; i++)
        {
            if (Contains(worldPoint, PortalWorldToLocal[i], PortalExtents[i]))
            {
                return GetPortalRoom(worldPoint, PortalWorldToLocal[i], PortalRoomA[i], PortalRoomB[i]);
            }
        }

        return -1;
    }
}
