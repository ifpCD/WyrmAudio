using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class AmbiNode : AmbiMonoBehaviour<AmbiNode>
{
    internal static TransformAccessArray Transforms;

    internal static NativeArray<float3> Positions;
    internal static NativeArray<quaternion> Quaternions;

    internal static NativeArray<float3> NodeExtents;

    internal static NativeReference<int> ListenerRoomID;

    internal static NativeArray<byte> IsInRooms;

    protected virtual void OnEnable() => Register();

    protected virtual void OnDisable() => Deregister();

    // csharpier-ignore
    protected override void AllocateNative()
    {
        NodeExtents    = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms      = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Positions      = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Quaternions    = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Transforms     = new(capacity: MaximumCapacity);
    }

    // Should be auto-generated
    protected override void DeallocateNative()
    {
        NodeExtents.TryDispose();
        ListenerRoomID.TryDispose();
        IsInRooms.TryDispose();
        Positions.TryDispose();
        Quaternions.TryDispose();
        Transforms.TryDispose();
    }

    // User should write the two methods below in order to deal with custom indices, matrices, etc.
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
            NodeExtents[removedIndex] = NodeExtents[lastIndex];

        Transforms.RemoveAtSwapBack(removedIndex);
    }

    protected override void LoadObjectToArrays()
    {
        NodeExtents[NativeIndex] = ColliderExtents;
        Transforms.Add(transform);
    }
}
