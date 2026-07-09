using Unity.Mathematics;

public struct RoomData
{
    public int roomIdentifier;
    public float3 center;
    public float3 extents; // Half-size for OBB check
    public quaternion rotation;
}

public struct PortalData
{
    public float3 center;
    public float3 extents;
    public float3 forward; // Useful for calculating sound direction passing through
    public quaternion rotation;
    public int roomA;
    public int roomB;
    public float openness;
}

public struct RoomAcousticMap
{
    public float totalDistance;
    public float3 eqAccumulation; // x=low, y=mid, z=high
    public int exitPortalIndex;   // The portal the sound takes to leave the listener's room
}