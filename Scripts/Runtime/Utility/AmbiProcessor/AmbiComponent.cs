using System;
using UnityEngine;

public abstract class AmbiBase<T>
    where T : AmbiBase<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;

    const int INACTIVE = -1;

    internal int SoAIndex { get; private set; } = INACTIVE;

    internal static T[] RegisteredInstances { get; private set; }

    public static int ActiveCount { get; private set; }

    public static bool CompletelyInactive => ActiveCount == 0;

    public bool IsRegistered => SoAIndex >= 0;

    protected abstract int AllocatedCapacity { get; }

    protected virtual bool RetainNativeWhenEmpty => false;

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadObjectToArrays();

    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = AllocatedCapacity;
        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];
        _allocationOwner = (T)this;
        AllocateNative();
    }

    protected void TriggerSync()
    {
        if (IsRegistered)
            LoadObjectToArrays();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        while (ActiveCount != 0)
            RegisteredInstances[ActiveCount - 1].Deregister();

        _allocationOwner.DeallocateNative();
        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    internal void Register()
    {
        if (SoAIndex >= 0)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        SoAIndex = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;
        LoadObjectToArrays();
    }

    internal void Deregister()
    {
        if (SoAIndex < 0)
            return;

        int removedIndex = SoAIndex;

        int lastIndex = --ActiveCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = RegisteredInstances[lastIndex];

            RegisteredInstances[removedIndex] = movedInstance;
            movedInstance.SoAIndex = removedIndex;
        }

        RemoveAtSwapBack(removedIndex, lastIndex);

        RegisteredInstances[lastIndex] = null;
        SoAIndex = INACTIVE;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}

public abstract class AmbiMonoBehaviour<T> : MonoBehaviour
    where T : AmbiMonoBehaviour<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;

    public const int INACTIVE = -1;

    internal int SoAIndex { get; private set; } = INACTIVE;

    internal static T[] RegisteredInstances { get; private set; }

    public static int ActiveCount { get; private set; }

    public static bool CompletelyInactive => ActiveCount == 0;

    public bool IsRegistered => SoAIndex >= 0;

    protected abstract int AllocatedCapacity { get; }

    protected virtual bool RetainNativeWhenEmpty => false;

    protected abstract void AllocateNative();

    protected abstract void DeallocateNative();

    protected abstract void RemoveAtSwapBack(int removedIndex, int lastIndex);

    protected abstract void LoadObjectToArrays();

    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = AllocatedCapacity;
        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];
        _allocationOwner = (T)this;
        AllocateNative();
    }

    protected void SoASync()
    {
        if (IsRegistered)
            LoadObjectToArrays();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        while (ActiveCount != 0)
            RegisteredInstances[ActiveCount - 1].Deregister();

        _allocationOwner.DeallocateNative();
        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    internal void Register()
    {
        if (SoAIndex >= 0)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Enabled {typeof(T).Name} capacity of {_maximumCapacity} was exceeded.");

        SoAIndex = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;
        LoadObjectToArrays();
    }

    internal void Deregister()
    {
        if (SoAIndex < 0)
            return;

        int removedIndex = SoAIndex;

        int lastIndex = --ActiveCount;

        if (removedIndex != lastIndex)
        {
            T movedInstance = RegisteredInstances[lastIndex];

            RegisteredInstances[removedIndex] = movedInstance;
            movedInstance.SoAIndex = removedIndex;
        }

        RemoveAtSwapBack(removedIndex, lastIndex);

        RegisteredInstances[lastIndex] = null;
        SoAIndex = INACTIVE;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}
