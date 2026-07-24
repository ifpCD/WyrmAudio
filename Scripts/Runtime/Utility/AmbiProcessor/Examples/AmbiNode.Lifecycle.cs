using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class AmbiNode : AmbiComponent<AmbiNode>
{
    internal static TransformAccessArray Transforms;
    internal static NativeArray<float3> Positions;
    internal static NativeArray<quaternion> Quaternions;

    internal static NativeArray<float3> NodeExtents;

    internal static NativeReference<int> ListenerRoomID;

    internal static NativeArray<byte> IsInRooms;

    protected virtual void OnEnable() => Register();

    protected virtual void OnDisable() => Deregister();

    protected override void AllocateNative()
    {
        NodeExtents = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Positions = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Quaternions = new(length: MaximumCapacity, allocator: Allocator.Persistent);
        Transforms = new(capacity: MaximumCapacity);
    }

    // Should be auto-generated
    protected override void DeallocateNative()
    {
        if (NodeExtents.IsCreated)
            NodeExtents.Dispose();
        if (ListenerRoomID.IsCreated)
            ListenerRoomID.Dispose();
        if (IsInRooms.IsCreated)
            IsInRooms.Dispose();
        if (Positions.IsCreated)
            Positions.Dispose();
        if (Quaternions.IsCreated)
            Quaternions.Dispose();
        if (Transforms.isCreated)
            Transforms.Dispose();
    }

    // User should write the two methods below in order to deal with custom indices, matrices, etc.
    protected override void RemoveNativeAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
            NodeExtents[removedIndex] = NodeExtents[lastIndex];

        Transforms.RemoveAtSwapBack(removedIndex);
    }

    protected override void LoadManagedToNative()
    {
        NodeExtents[EnabledIndex] = ColliderExtents;
        Transforms.Add(transform);
    }
}
