using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class WyrmRoomManager
{
#if UNITY_EDITOR
    [Header("Gizmos & Debugging")]
    [Tooltip("Draws the bounds of all initialized acoustic rooms. Highlights the listener's current room.")]
    public bool drawRooms = true;

    [Tooltip("Draws the bounds of all portals, colored by their openness.")]
    public bool drawPortals = true;

    [Tooltip("Draws the shortest path tree from the listener out to all connected rooms.")]
    public bool drawAcousticGraph = false;

    [Tooltip("Draws the multi-room path a sound takes to reach the listener.")]
    public bool drawSourcePropagation = true;

    [Tooltip("Draws direct line of sight raycasts for occlusion, colored by hit state.")]
    public bool drawSourceOcclusion = true;

    [Tooltip("Shows text labels for Room IDs, Portal data, EQ Accumulation, and Distances.")]
    public bool drawLabels = true;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !Rooms.IsCreated) return;

        int listenerRoom = ListenerRoomIdentifier.IsCreated ? ListenerRoomIdentifier.Value : -1;

        DrawRooms(listenerRoom);
        DrawPortals();
        DrawAcousticGraph(listenerRoom);
        DrawSourcesAndPropagation(listenerRoom);
    }

    private void DrawRooms(int listenerRoom)
    {
        if (!drawRooms) return;

        for (int i = 0; i < Rooms.Length; i++)
        {
            var room = Rooms[i];
            bool isListenerHere = i == listenerRoom;

            Gizmos.matrix = Matrix4x4.TRS(room.center, room.rotation, Vector3.one);

            // Highlight the room the listener is currently standing in
            Gizmos.color = isListenerHere ? new Color(0, 1, 0, 0.15f) : new Color(0, 0.5f, 1, 0.05f);
            Gizmos.DrawCube(Vector3.zero, room.extents * 2f);

            Gizmos.color = isListenerHere ? Color.green : new Color(0, 0.5f, 1, 0.5f);
            Gizmos.DrawWireCube(Vector3.zero, room.extents * 2f);

            Gizmos.matrix = Matrix4x4.identity;

            if (drawLabels)
            {
                Handles.Label(room.center, $"Room {room.roomIdentifier}\nIndex: {i}");
            }
        }
    }

    private void DrawPortals()
    {
        if (!drawPortals || !Portals.IsCreated) return;

        for (int i = 0; i < Portals.Length; i++)
        {
            var portal = Portals[i];
            Gizmos.matrix = Matrix4x4.TRS(portal.center, portal.rotation, Vector3.one);

            // Red = Closed, Cyan = Open
            Color portalColor = Color.Lerp(new Color(1, 0, 0, 0.5f), new Color(0, 1, 1, 0.5f), portal.openness);
            Gizmos.color = portalColor;
            Gizmos.DrawCube(Vector3.zero, portal.extents * 2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, portal.extents * 2f);

            // forward direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Vector3.zero, 1.5f * portal.extents.z * Vector3.forward);

            Gizmos.matrix = Matrix4x4.identity;

            if (drawLabels)
            {
                Handles.Label(portal.center, $"Portal {i}\nRooms: {portal.roomA} <-> {portal.roomB}\nOpenness: {portal.openness:F2}");
            }
        }
    }

    private void DrawAcousticGraph(int listenerRoom)
    {
        if (!drawAcousticGraph || !AcousticMap.IsCreated || listenerRoom == -1) return;

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(ListenerPosition, 0.15f);

        for (int i = 0; i < AcousticMap.Length; i++)
        {
            if (i == listenerRoom) continue; // Listener's room is distance 0

            var map = AcousticMap[i];
            if (map.exitPortalIndex != -1)
            {
                var portal = Portals[map.exitPortalIndex];

                // Draw path from Room Center -> Exit Portal
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
                Gizmos.DrawLine(Rooms[i].center, portal.center);

                // Draw path from Exit Portal -> Next Room Center
                int nextRoom = (portal.roomA == i) ? portal.roomB : portal.roomA;
                if (nextRoom != -1)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Faded Orange
                    Gizmos.DrawLine(portal.center, Rooms[nextRoom].center);
                }

                if (drawLabels)
                {
                    Vector3 labelPos = Rooms[i].center + new float3(0, 1f, 0);
                    Handles.Label(labelPos, $"Dist to Listener: {map.totalDistance:F2}m\nEQ: {map.eqAccumulation}");
                }
            }
        }
    }

    private void DrawSourcesAndPropagation(int listenerRoom)
    {
        var pool = WyrmPoolController.Instance;
        if (pool == null || pool.ActiveCount == 0 || !pool.SourcePositions.IsCreated) return;

        for (int i = 0; i < pool.ActiveCount; i++)
        {
            float3 sourcePosition = pool.SourcePositions[i];
            int sourceRoom = pool.SourceRoomIdentifiers[i];
            float sourceOcclusion = pool.TargetOcclusion01s[i];

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(sourcePosition, 0.1f);

            if (drawLabels)
            {
                string label = $"Source {i}\nRoom: {sourceRoom}";
                if (drawSourceOcclusion) label += $"\nOcclusion: {sourceOcclusion:F2}";
                Handles.Label(sourcePosition + new float3(0, 0.5f, 0), label);
            }

            if (drawSourceOcclusion)
            {
                Gizmos.color = sourceOcclusion > 0.5f ? new Color(0, 1, 0, 0.6f) : new Color(1, 0, 0, 0.6f);
                DrawDashedLine(sourcePosition, ListenerPosition, 0.5f);
            }

            if (drawSourcePropagation)
            {
                Gizmos.color = Color.magenta;

                if (sourceRoom == -1 || !AcousticMap.IsCreated)
                {
                    // Fallback direct path (source is out of bounds)
                    Gizmos.DrawLine(sourcePosition, ListenerPosition);
                    continue;
                }

                float3 currentPos = sourcePosition;
                int currentRoom = sourceRoom;
                int safeGuard = 0;

                while (currentRoom != listenerRoom && safeGuard < 32)
                {
                    var map = AcousticMap[currentRoom];
                    if (map.exitPortalIndex == -1) break;

                    var portal = Portals[map.exitPortalIndex];

                    // draw segment to portal
                    Gizmos.DrawLine(currentPos, portal.center);

                    // Hop to next room
                    currentPos = portal.center;
                    currentRoom = portal.roomA == currentRoom ? portal.roomB : portal.roomA;
                    safeGuard++;
                }

                // Final segment from the last portal to the listener
                if (currentRoom == listenerRoom || safeGuard >= 32)
                {
                    Gizmos.DrawLine(currentPos, ListenerPosition);
                }

                // 4. Visualize the calculated Direction Vector at the Listener
                float3 propDir = pool.PropagationDirections[i];
                if (math.lengthsq(propDir) > 0.001f)
                {
                    Gizmos.color = Color.blue;
                    float3 dirNorm = math.normalize(propDir);

                    // "arriving sound" vector ray
                    Gizmos.DrawRay(ListenerPosition, -dirNorm * 1.5f);
                }
            }
        }
    }

    private void DrawDashedLine(Vector3 start, Vector3 end, float dashLength)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        direction.Normalize();

        int numDashes = Mathf.FloorToInt(distance / dashLength);
        for (int i = 0; i < numDashes; i += 2)
        {
            Vector3 p1 = start + direction * (i * dashLength);
            Vector3 p2 = start + direction * ((i + 1) * dashLength);

            // Clamp the last dash to the end point
            if (i == numDashes - 1 || i == numDashes - 2) p2 = end;

            Gizmos.DrawLine(p1, p2);
        }
    }
#endif
}