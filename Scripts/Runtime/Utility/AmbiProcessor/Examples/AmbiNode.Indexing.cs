using System;
using UnityEngine;

public partial class AmbiNode : MonoBehaviour
{
    internal const int MAXIMUM_CAPACITY = 2000;

    static readonly AmbiNode[] EnabledNodes = new AmbiNode[MAXIMUM_CAPACITY];

    static int _enabledNodeCount;

    internal int _index = -1;

    protected virtual void OnEnable() => Register();

    protected virtual void OnDisable() => Deregister();

    void Register()
    {
        if (_index >= 0)
            return;

        if (_enabledNodeCount == MAXIMUM_CAPACITY)
            throw new InvalidOperationException($"Enabled AmbiNode capacity of {MAXIMUM_CAPACITY} was exceeded.");

        if (_enabledNodeCount == 0)
            Allocate();

        _index = _enabledNodeCount;
        EnabledNodes[_enabledNodeCount++] = this;
        LoadManagedToNative();
    }

    void Deregister()
    {
        var removedIndex = _index;

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
        _index = -1;

        if (_enabledNodeCount == 0)
            Deallocate();
    }
}
