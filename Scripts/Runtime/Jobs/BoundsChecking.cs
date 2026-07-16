using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct LocateSourcesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> SourcePositions;
    [ReadOnly] public NativeArray<RoomData> Rooms;

    [WriteOnly] public NativeArray<int> SourceRoomIdentifiers;

    public void Execute(int index)
    {
        SourceRoomIdentifiers[index] = -1;
        var sourcePosition = SourcePositions[index];

        for (var roomIndex = 0; roomIndex < Rooms.Length; roomIndex++)
        {
            if (WyrmPathingMath.IsPointInRoom(sourcePosition, Rooms[roomIndex]))
            {
                SourceRoomIdentifiers[index] = roomIndex;
                return;
            }
        }
    }
}

[BurstCompile]
public struct LocateListenerJob : IJob
{
    [ReadOnly] public float3 ListenerPosition;
    [ReadOnly] public NativeArray<RoomData> Rooms;

    [WriteOnly] public NativeReference<int> ListenerRoomIdentifier;

    public void Execute()
    {
        ListenerRoomIdentifier.Value = -1;

        for (int roomIndex = 0; roomIndex < Rooms.Length; roomIndex++)
        {
            if (WyrmPathingMath.IsPointInRoom(ListenerPosition, Rooms[roomIndex]))
            {
                ListenerRoomIdentifier.Value = roomIndex;
                return;
            }
        }
    }
}