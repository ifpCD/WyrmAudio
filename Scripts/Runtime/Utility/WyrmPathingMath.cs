using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Burst;

public static class WyrmPathingMath
{
    [BurstCompile]
    public static bool IsPointInRoom(float3 worldPoint, in RoomData room)
    {
        float3 localPos = math.mul(math.inverse(room.rotation), worldPoint - room.center);

        return math.abs(localPos.x) <= room.extents.x &&
               math.abs(localPos.y) <= room.extents.y &&
               math.abs(localPos.z) <= room.extents.z;
    }
}