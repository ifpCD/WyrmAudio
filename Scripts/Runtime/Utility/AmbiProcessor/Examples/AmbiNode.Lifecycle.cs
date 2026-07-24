using System;
using Unity.Collections;
using UnityEngine;

public partial class AmbiNode : MonoBehaviour
{
    internal const int MAXIMUM_CAPACITY = 2000;

    static readonly AmbiNode[] EnabledNodes = new AmbiNode[MAXIMUM_CAPACITY];

    static int _enabledNodeCount;

    internal int _index = -1;

    void OnEnable()
    {
        Register(this);
        AmbiOnEnable();
    }

    void OnDisable()
    {
        Deregister(this);
        AmbiOnDisable();
    }

    void OnDestroy()
    {
        Deregister(this);
        AmbiOnDestroy();
    }

    void Update()
    {
        if (_index != 0)
            return;

        for (var i = 0; i < _enabledNodeCount; i++)
            EnabledNodes[i].PreBatchUpdate();

        BatchUpdate();

        for (var i = 0; i < _enabledNodeCount; i++)
            EnabledNodes[i].PostBatchUpdate();
    }

    static void Register(AmbiNode instance)
    {
        if (instance._index >= 0)
            return;

        if (_enabledNodeCount == MAXIMUM_CAPACITY)
            throw new InvalidOperationException($"Enabled AmbiNode capacity of {MAXIMUM_CAPACITY} was exceeded.");

        if (_enabledNodeCount == 0)
            Allocate();

        instance._index = _enabledNodeCount;
        EnabledNodes[_enabledNodeCount++] = instance;
        instance.LoadManagedToNative();
    }

    static void Deregister(AmbiNode instance)
    {
        var removedIndex = instance._index;

        if (removedIndex < 0)
            return;

        var lastIndex = --_enabledNodeCount;

        if (removedIndex != lastIndex)
        {
            var movedInstance = EnabledNodes[lastIndex];

            EnabledNodes[removedIndex] = movedInstance;
            movedInstance._index = removedIndex;
            SwapBackOnDisable(removedIndex, lastIndex);
        }

        EnabledNodes[lastIndex] = null;
        instance._index = -1;

        if (_enabledNodeCount == 0)
            Deallocate();
    }

    static void Allocate()
    {
        NodeExtents = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        Transforms = new(capacity: MAXIMUM_CAPACITY);
    }

    static void Deallocate()
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

    static void SwapBackOnDisable(int removedIndex, int lastIndex)
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
