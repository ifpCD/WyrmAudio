using Unity.Collections;
using UnityEngine;

public partial class AmbiNode : MonoBehaviour
{
    void Allocate()
    {
        NodeExtents = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        Transforms = new(capacity: MAXIMUM_CAPACITY);
    }

    void Deallocate()
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

    // No point in copying over scratch buffer arrays
    void SwapBackOnDisable(int removedIndex, int lastIndex)
    {
        NodeExtents[removedIndex] = NodeExtents[lastIndex];
        Transforms[removedIndex] = Transforms[lastIndex];
    }

    void LoadManagedToNative()
    {
        NodeExtents[_index] = ColliderExtents;
        Transforms[_index] = transform;
    }
}
