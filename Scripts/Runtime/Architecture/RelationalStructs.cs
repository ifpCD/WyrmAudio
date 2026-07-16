using Unity.Collections;
using Unity.Mathematics;

// First we check Listener and Source locations
public struct PartialRoomShapeData
{
    public float3 extents;

    public float4x4 worldToLocal;

    public int roomIdentifier;
}
// if we can't find it, we search in Portals (they will overlap with rooms)
// and if we find them there we will use the forward direction to calculate which side of the portal they are in, and then use A/B rooms respectively
public struct PortalData
{
    public float4x4 worldToLocal;

    public float3 extents;

    public int roomA;
    public int roomB;
}

public struct RoomData
{
}

// what will go directly into Spherical Harmonic Coefficient Calculator
public struct SourcePropagationData
{
    public float3 direction;
    public float spatialWidth;
    public float distance;
}