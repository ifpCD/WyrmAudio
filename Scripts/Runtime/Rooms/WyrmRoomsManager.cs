using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

internal class WyrmRoomManager : MonoBehaviour
{
    public NativeArray<byte> ListenerInRoomState;
    
    public NativeArray<float4x4> RoomWorldToLocal;
    public NativeArray<float3> RoomExtents;

    private WyrmAudioRoom[] _activeRooms;
    private int _activeCount;
}