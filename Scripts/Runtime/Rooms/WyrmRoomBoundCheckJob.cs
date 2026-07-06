using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct WyrmRoomBoundCheckJob : IJobParallelFor
{
    [ReadOnly] public float3 ListenerPosition;
    [ReadOnly] public NativeArray<float4x4> RoomWorldToLocal;
    [ReadOnly] public NativeArray<float3> RoomExtents;

    [WriteOnly] public NativeArray<byte> ListenerInRoomState;

    public void Execute(int index)
    {
        for (int i = 0; i < RoomWorldToLocal.Length; i++)
        {
            float3 localPos = math.transform(RoomWorldToLocal[i], ListenerPosition);
            float3 extents = RoomExtents[i];

            if (math.abs(localPos.x) <= extents.x &&
                math.abs(localPos.y) <= extents.y &&
                math.abs(localPos.z) <= extents.z)
            {
                ListenerInRoomState[i] = 1;
                break;
            }

            ListenerInRoomState[i] = 0;
        }
    }
}