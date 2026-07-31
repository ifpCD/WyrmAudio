using System;
using UnityEngine;

public abstract class AmbiBase<T>
    where T : AmbiBase<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;

    protected int NativeIndex { get; private set; } = -1;

    internal static T[] RegisteredInstances { get; private set; }

    public static int ActiveCount { get; private set; }

    public static bool CompletelyInactive => ActiveCount == 0;

    public bool IsRegistered => NativeIndex != -1;

    protected abstract int MaximumCapacity { get; }

    protected virtual bool RetainNativeWhenEmpty => false;

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveNativeAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadManagedToNative();

    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = MaximumCapacity;
        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];
        _allocationOwner = (T)this;
        AllocateNative();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        if (ActiveCount != 0)
            throw new InvalidOperationException($"{typeof(T).Name} cannot dispose its registry while {ActiveCount} instances are registered.");

        _allocationOwner.DeallocateNative();
        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    internal void Register()
    {
        if (NativeIndex >= 0)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        NativeIndex = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;
        LoadManagedToNative();
    }

    internal void Deregister()
    {
        int removedIndex = NativeIndex;

        if (removedIndex < 0)
            return;

        int lastIndex = --ActiveCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = RegisteredInstances[lastIndex];

            RegisteredInstances[removedIndex] = movedInstance;
            movedInstance.NativeIndex = removedIndex;
        }

        RemoveNativeAtSwapBack(removedIndex, lastIndex);

        RegisteredInstances[lastIndex] = null;
        NativeIndex = -1;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}

public abstract class AmbiMonoBehaviour<T> : MonoBehaviour
    where T : AmbiMonoBehaviour<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;

    protected int NativeIndex { get; private set; } = -1;

    internal static T[] RegisteredInstances { get; private set; }

    public static int ActiveCount { get; private set; }

    public static bool CompletelyInactive => ActiveCount == 0;

    public bool IsRegistered => NativeIndex != -1;

    protected abstract int MaximumCapacity { get; }

    protected virtual bool RetainNativeWhenEmpty => false;

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadObjectToArrays();

    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = MaximumCapacity;
        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];
        _allocationOwner = (T)this;
        AllocateNative();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        if (ActiveCount != 0)
            throw new InvalidOperationException($"{typeof(T).Name} cannot dispose its registry while {ActiveCount} instances are registered.");

        _allocationOwner.DeallocateNative();
        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    internal void Register()
    {
        if (NativeIndex >= 0)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        NativeIndex = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;
        LoadObjectToArrays();
    }

    internal void Deregister()
    {
        if (NativeIndex < 0)
            return;

        int removedIndex = NativeIndex;

        int lastIndex = --ActiveCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = RegisteredInstances[lastIndex];

            RegisteredInstances[removedIndex] = movedInstance;
            movedInstance.NativeIndex = removedIndex;
        }

        RemoveAtSwapBack(removedIndex, lastIndex);

        RegisteredInstances[lastIndex] = null;
        NativeIndex = -1;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}
