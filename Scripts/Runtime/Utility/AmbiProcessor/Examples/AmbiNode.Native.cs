using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

// User written
[RequireComponent(typeof(BoxCollider))]
public partial class AmbiNode : MonoBehaviour
{
    internal static NativeArray<float3> NodeExtents;

    internal static NativeReference<int> ListenerRoomID;

    internal static NativeArray<byte> IsInRooms;

    internal static TransformAccessArray Transforms;

    internal static NativeArray<float3> Positions;
    internal static NativeArray<quaternion> Quaternions;
}
