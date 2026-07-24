using System;
using Codice.CM.Client.Differences.Graphic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

// User written
[RequireComponent(typeof(BoxCollider))]
public partial class AmbiNode : MonoBehaviour
{
    // BATCH SECTION
    internal static NativeArray<float3> NodeExtents;

    internal static NativeReference<int> ListenerRoomID;

    internal static NativeArray<byte> IsInRooms;

    internal static TransformAccessArray Transforms;

    internal static NativeArray<float3> Positions;
    internal static NativeArray<quaternion> Quaternions;

    // parallel jobs, unsafe pointer calls into C++, etc here
    internal static void BatchUpdate() { }

    // MANAGED SECTION
    public BoxCollider BoxCollider { get; private set; }

    public Vector3 Position
    {
        get => Positions.GetOrDefault(_index);
    }

    public Quaternion Rotation
    {
        get => Quaternions.GetOrDefault(_index);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    public bool IsInRoom
    {
        get => Convert.ToBoolean(IsInRooms.GetOrDefault(_index));
    }

    void AmbiOnEnable() { }

    internal void PreBatchUpdate() { }

    internal void PostBatchUpdate() { }

    void AmbiOnDisable() { }

    void AmbiOnDestroy() { }
}
