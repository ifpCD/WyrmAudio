using System;
using UnityEngine;

internal class AmbiProcessor : MonoBehaviour
{
    static AmbiProcessor _instance;

    AmbiNode[] _graphNodeReferences;

    int _enabledGraphNodeAmount;

    static AmbiProcessor GetOrCreate()
    {
        if (_instance != null)
            return _instance;

        AmbiNode.Allocate();

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
        AmbiNode.Deallocate();
    }
}
