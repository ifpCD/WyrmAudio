using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class WyrmRoomManager : MonoBehaviour
{
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

    [BurstCompile]
    public struct CalculateAcousticMapJob : IJob
    {
        [ReadOnly] public NativeReference<int> ListenerRoomIdentifier;
        [ReadOnly] public NativeArray<RoomData> Rooms;
        [ReadOnly] public NativeArray<PortalData> Portals;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> RoomToPortals;

        public NativeArray<RoomAcousticMap> AcousticMap;

        public void Execute()
        {
            int startRoom = ListenerRoomIdentifier.Value;

            for (int i = 0; i < AcousticMap.Length; i++)
            {
                AcousticMap[i] = new RoomAcousticMap 
                { 
                    totalDistance = float.MaxValue, 
                    eqAccumulation = new float3(1, 1, 1), 
                    exitPortalIndex = -1 
                };
            }

            if (startRoom < 0 || startRoom >= Rooms.Length) return;

            AcousticMap[startRoom] = new RoomAcousticMap 
            { 
                totalDistance = 0, 
                eqAccumulation = new float3(1, 1, 1), 
                exitPortalIndex = -1 
            };

            // these will need to go at some point
            NativeQueue<int> queue = new(Allocator.Temp);
            NativeArray<bool> inQueue = new(Rooms.Length, Allocator.Temp);

            queue.Enqueue(startRoom);
            inQueue[startRoom] = true;

            while (queue.TryDequeue(out int currRoom))
            {
                inQueue[currRoom] = false;
                RoomAcousticMap currMap = AcousticMap[currRoom];

                if (RoomToPortals.TryGetFirstValue(currRoom, out int portalIndex, out var iterator))
                {
                    do
                    {
                        var portal = Portals[portalIndex];
                        if (portal.openness <= 0.001f) continue; 

                        int nextRoom = (portal.roomA == currRoom) ? portal.roomB : portal.roomA;
                        if (nextRoom == -1) continue;

                        float distToPortal = math.distance(Rooms[currRoom].center, portal.center);
                        float distFromPortal = math.distance(portal.center, Rooms[nextRoom].center);
                        float nextDist = currMap.totalDistance + distToPortal + distFromPortal;

                        if (nextDist < AcousticMap[nextRoom].totalDistance)
                        {
                            int exitPortal = (currRoom == startRoom) ? portalIndex : currMap.exitPortalIndex;
                            float3 nextEq = currMap.eqAccumulation * portal.openness;

                            AcousticMap[nextRoom] = new RoomAcousticMap
                            {
                                totalDistance = nextDist,
                                eqAccumulation = nextEq,
                                exitPortalIndex = exitPortal
                            };

                            if (!inQueue[nextRoom])
                            {
                                queue.Enqueue(nextRoom);
                                inQueue[nextRoom] = true;
                            }
                        }
                    }
                    while (RoomToPortals.TryGetNextValue(out portalIndex, ref iterator));
                }
            }

            queue.Dispose();
            inQueue.Dispose();
        }
    }

    [BurstCompile]
    public struct ResolvePropagationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> SourceRoomIdentifiers;
        [ReadOnly] public NativeArray<float3> SourcePositions;
        [ReadOnly] public NativeArray<RoomAcousticMap> AcousticMap;
        [ReadOnly] public NativeArray<PortalData> Portals;
        [ReadOnly] public float3 ListenerPosition;

        [WriteOnly] public NativeArray<float3> PropagationDirections;
        [WriteOnly] public NativeArray<float> PropagationDistances;
        [WriteOnly] public NativeArray<float3> PropagationPathEQs;

        public void Execute(int index)
        {
            int sourceRoom = SourceRoomIdentifiers[index];
            if (sourceRoom == -1)
            {
                // Fallback direct path if out-of-bounds or lost
                PropagationDirections[index] = SourcePositions[index] - ListenerPosition;
                PropagationDistances[index] = math.distance(SourcePositions[index], ListenerPosition);
                PropagationPathEQs[index] = new float3(1, 1, 1);
                return;
            }

            var mapData = AcousticMap[sourceRoom];

            if (mapData.exitPortalIndex == -1)
            {
                PropagationDirections[index] = SourcePositions[index] - ListenerPosition;
                PropagationDistances[index] = math.distance(SourcePositions[index], ListenerPosition);
                PropagationPathEQs[index] = new float3(1, 1, 1);
            }
            else 
            {
                var portal = Portals[mapData.exitPortalIndex];
                PropagationDirections[index] = portal.center - ListenerPosition;

                float distToPortal = math.distance(ListenerPosition, portal.center);
                PropagationDistances[index] = distToPortal + mapData.totalDistance;

                PropagationPathEQs[index] = mapData.eqAccumulation;
            }
        }
    }

    [BurstCompile]
    public struct PrepareOcclusionRaycastsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> SourcePositions;
        [ReadOnly] public float3 ListenerPosition;
        [ReadOnly] public int LayerMask;

        [WriteOnly] public NativeArray<RaycastCommand> RaycastCommands;

        public void Execute(int index)
        {
            float3 src = SourcePositions[index];
            float3 dir = ListenerPosition - src;
            float dist = math.length(dir);

            if (dist > 0.001f)
            {
                // Avoid zero length cast issues
                float3 dirNorm = dir / dist;
                RaycastCommands[index] = new RaycastCommand(src, dirNorm, new QueryParameters(LayerMask, false, QueryTriggerInteraction.Ignore), dist);
            }
            else
            {
                RaycastCommands[index] = new RaycastCommand();
            }
        }
    }

    [BurstCompile]
    public struct ResolveOcclusionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RaycastHit> RaycastHits;

        [WriteOnly] public NativeArray<float> SourceOcclusions;

        public void Execute(int index)
        {
            bool occluded = RaycastHits[index].colliderInstanceID != 0;
            SourceOcclusions[index] = occluded ? 0f : 1f;
        }
    }
}