using System;
using Codice.CM.Common;
using Unity.Collections;
using UnityEngine;

public partial class AmbiNode : MonoBehaviour
{
    internal const int MAXIMUM_CAPACITY = 2000;

    internal int _index = -1;

    void OnEnable()
    {
        AmbiProcessor.InstanceOnEnableNotification(this);
        OnEnable();
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

    internal static void AmbiAllocate()
    {
        NodeExtents = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        ListenerRoomID = new(allocator: Allocator.Persistent);
        IsInRooms = new(length: MAXIMUM_CAPACITY, allocator: Allocator.Persistent);
        Transforms = new(capacity: MAXIMUM_CAPACITY);
    }

    internal static void AmbiDeallocate()
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

internal class AmbiProcessor : MonoBehaviour
{
    static AmbiProcessor _instance;

    AmbiNode[] _graphNodeReferences;

    int _enabledGraphNodeAmount;

    static AmbiProcessor GetOrCreate()
    {
        if (_instance != null)
            return _instance;

        AmbiNode.AmbiAllocate();

        var processorObject = new GameObject(nameof(AmbiProcessor)) { hideFlags = HideFlags.HideInHierarchy };

        DontDestroyOnLoad(processorObject);

        _instance = processorObject.AddComponent<AmbiProcessor>();
        _instance._graphNodeReferences = new AmbiNode[AmbiNode.MAXIMUM_CAPACITY];

        return _instance;
    }

    internal static void InstanceOnEnableNotification(AmbiNode instance)
    {
        var processor = GetOrCreate();
        processor.Register(instance);
    }

    internal static void InstanceOnDisableNotification(AmbiNode instance)
    {
        if (_instance != null)
            _instance.Deregister(instance);
    }

    internal static void InstanceOnDestroyNotification(AmbiNode instance)
    {
        if (_instance != null)
            _instance.Deregister(instance);
    }

    void Register(AmbiNode instance)
    {
        if (instance._index < _enabledGraphNodeAmount)
            return;

        if (_enabledGraphNodeAmount == _graphNodeReferences.Length)
            throw new InvalidOperationException($"Enabled GraphNode capacity of {AmbiNode.MAXIMUM_CAPACITY} was exceeded.");

        var index = _enabledGraphNodeAmount++;

        instance._index = index;
        _graphNodeReferences[index] = instance;
        instance.LoadManagedToNative();
    }

    void Deregister(AmbiNode instance)
    {
        var deactivatedIndex = instance._index;

        if (deactivatedIndex < 0)
            return;

        var lastIndex = --_enabledGraphNodeAmount;

        if (deactivatedIndex != lastIndex)
        {
            var movedInstance = _graphNodeReferences[lastIndex];

            _graphNodeReferences[deactivatedIndex] = movedInstance;
            movedInstance._index = deactivatedIndex;

            _graphNodeReferences[lastIndex] = instance;
        }

        instance._index = lastIndex;

        if (deactivatedIndex != lastIndex)
            AmbiNode.SwapBackOnDisable(deactivatedIndex, lastIndex);
    }

    // if this processor is connected to a parent scheduler
    // this Update method can be overriden to do nothing
    protected virtual void Update()
    {
        PreBatchUpdate();
        AmbiNode.BatchUpdate();
        PostBatchUpdate();
    }

    public void PreBatchUpdate()
    {
        for (var i = 0; i < _enabledGraphNodeAmount; i++)
            _graphNodeReferences[i].PreBatchUpdate();
    }

    public void PostBatchUpdate()
    {
        for (var i = 0; i < _enabledGraphNodeAmount; i++)
            _graphNodeReferences[i].PostBatchUpdate();
    }

    void OnDestroy()
    {
        if (_instance != this)
            return;

        _instance = null;
        AmbiNode.AmbiDeallocate();
    }
}
