using System;
using UnityEngine;

/// <summary>
/// Maintains a densely packed registry for a concrete Ambi component type.
/// Native storage and synchronization remain the responsibility of the implementation.
/// </summary>
public abstract class AmbiComponent<T> : MonoBehaviour
    where T : AmbiComponent<T>
{
    static T[] _enabledInstances;
    static int _enabledInstanceCount;
    static int _maximumCapacity;

    int _index = -1;

    protected int EnabledIndex => _index;

    protected bool IsRegistered => _index != -1;

    protected static T[] EnabledInstances => _enabledInstances;

    public static int EnabledInstanceCount => _enabledInstanceCount;

    protected abstract int MaximumCapacity { get; }

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveNativeAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadManagedToNative();

    protected void Register()
    {
        if (_index >= 0)
            return;

        if (_enabledInstances == null)
        {
            _maximumCapacity = MaximumCapacity;
            _enabledInstances = new T[MaximumCapacity];
            return;
        }

        if (_enabledInstanceCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        if (_enabledInstanceCount == 0)
            AllocateNative();

        _index = _enabledInstanceCount;
        _enabledInstances[_enabledInstanceCount++] = (T)this;
        LoadManagedToNative();
    }

    protected void Deregister()
    {
        int removedIndex = _index;

        if (removedIndex < 0)
            return;

        int lastIndex = --_enabledInstanceCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = _enabledInstances[lastIndex];

            _enabledInstances[removedIndex] = movedInstance;
            movedInstance._index = removedIndex;
        }

        RemoveNativeAtSwapBack(removedIndex, lastIndex);

        _enabledInstances[lastIndex] = null;
        _index = -1;

        if (_enabledInstanceCount == 0)
            DeallocateNative();
    }
}
