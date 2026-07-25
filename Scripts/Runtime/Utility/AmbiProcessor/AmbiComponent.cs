using System;
using UnityEngine;

public abstract class AmbiComponent<T> : MonoBehaviour
    where T : AmbiComponent<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;

    protected int NativeIndex { get; private set; } = -1;

    protected static T[] EnabledInstances { get; private set; }

    public static int ActiveCount { get; private set; }

    public static bool CompletelyInactive => ActiveCount == 0;

    protected bool IsRegistered => NativeIndex != -1;

    protected abstract int MaximumCapacity { get; }

    protected virtual bool RetainNativeWhenEmpty => false;

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveNativeAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadManagedToNative();

    protected void InitializeRegistry()
    {
        if (EnabledInstances != null)
            return;

        _maximumCapacity = MaximumCapacity;
        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        EnabledInstances = new T[_maximumCapacity];
        _allocationOwner = (T)this;
        AllocateNative();
    }

    protected static void DisposeRegistry()
    {
        if (EnabledInstances == null)
            return;

        if (ActiveCount != 0)
            throw new InvalidOperationException($"{typeof(T).Name} cannot dispose its registry while {ActiveCount} instances are registered.");

        _allocationOwner.DeallocateNative();
        _allocationOwner = null;
        EnabledInstances = null;
        _maximumCapacity = 0;
    }

    protected void Register()
    {
        if (NativeIndex >= 0)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        NativeIndex = ActiveCount;
        EnabledInstances[ActiveCount++] = (T)this;
        LoadManagedToNative();
    }

    protected void Deregister()
    {
        int removedIndex = NativeIndex;

        if (removedIndex < 0)
            return;

        int lastIndex = --ActiveCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = EnabledInstances[lastIndex];

            EnabledInstances[removedIndex] = movedInstance;
            movedInstance.NativeIndex = removedIndex;
        }

        RemoveNativeAtSwapBack(removedIndex, lastIndex);

        EnabledInstances[lastIndex] = null;
        NativeIndex = -1;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}
