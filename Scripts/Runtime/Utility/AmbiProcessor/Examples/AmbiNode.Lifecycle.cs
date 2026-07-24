using Unity.Collections;
using UnityEngine;

public partial class AmbiNode : MonoBehaviour
{
    internal const int MAXIMUM_CAPACITY = 2000;

    internal int _index = -1;

    void OnEnable()
    {
        AmbiProcessor.InstanceOnEnableNotification(this);
        AmbiOnEnable();
    }

    void OnDisable()
    {
        AmbiProcessor.InstanceOnDisableNotification(this);
        AmbiOnDisable();
    }

    void OnDestroy()
    {
        AmbiProcessor.InstanceOnDestroyNotification(this);
        AmbiOnDestroy();
    }

    internal static void Allocate()
    {
        NodeExtents = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        Transforms = new(capacity: MAXIMUM_CAPACITY);
    }

    internal static void Deallocate()
    {
        if (NodeExtents.IsCreated)
            NodeExtents.Dispose();
        if (ListenerRoomID.IsCreated)
            ListenerRoomID.Dispose();
        if (IsInRooms.IsCreated)
            IsInRooms.Dispose();
        if (Transforms.isCreated)
            Transforms.Dispose();
    }

    internal static void SwapBackOnDisable(int removedIndex, int lastIndex)
    {
        NodeExtents[removedIndex] = NodeExtents[lastIndex];
        Transforms[removedIndex] = Transforms[lastIndex];
    }

    internal void LoadManagedToNative()
    {
        NodeExtents[_index] = ColliderExtents;
        Transforms[_index] = transform;
    }
}