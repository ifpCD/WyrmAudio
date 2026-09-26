using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
public abstract class AmbiBase<T>
    where T : AmbiBase<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;
    public const int INACTIVE = -1;

    public static NativeArray<int> HandleToSoA;
    public static NativeArray<int> SoAToHandle;
    public static NativeArray<int> HandleVersions;

    private static Queue<int> _freeHandleIndices;
    private static int _nextHandleIndex;

    public AmbiHandle Handle { get; private set; } = AmbiHandle.Null;
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

    // csharpier-ignore
    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = AllocatedCapacity;

        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];

        HandleToSoA         = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        SoAToHandle         = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        HandleVersions      = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        _freeHandleIndices  = new Queue<int>(_maximumCapacity);
        _nextHandleIndex    = 0;

        _allocationOwner    = (T)this;
        AllocateNative();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        while (ActiveCount != 0)
            RegisteredInstances[ActiveCount - 1].Deregister();

        _allocationOwner.DeallocateNative();

        HandleToSoA.TryDispose();
        SoAToHandle.TryDispose();
        HandleVersions.TryDispose();
        _freeHandleIndices = null;

        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    // csharpier-ignore
    internal void Register()
    {
        if (IsRegistered)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Capacity of {_maximumCapacity} exceeded.");

        int handleIndex                    = _freeHandleIndices.Count > 0 ? _freeHandleIndices.Dequeue() : _nextHandleIndex++;
        int version                        = HandleVersions[handleIndex];
        Handle                             = new AmbiHandle { Index = handleIndex, Version = version };

        SoAIndex                           = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;

        HandleToSoA[handleIndex]           = SoAIndex;
        SoAToHandle[SoAIndex]              = handleIndex;

        LoadObjectToArrays();
    }

    // csharpier-ignore
    internal void Deregister()
    {
        if (!IsRegistered)
            return;

        int removedSoAIndex = SoAIndex;
        int lastSoAIndex = --ActiveCount;

        if (removedSoAIndex != lastSoAIndex)
        {
            T movedInstance                      = RegisteredInstances[lastSoAIndex];
            RegisteredInstances[removedSoAIndex] = movedInstance;
            movedInstance.SoAIndex               = removedSoAIndex;

            int movedHandleIndex                 = SoAToHandle[lastSoAIndex];
            HandleToSoA[movedHandleIndex]        = removedSoAIndex;
            SoAToHandle[removedSoAIndex]         = movedHandleIndex;
        }

        RemoveAtSwapBack(removedSoAIndex, lastSoAIndex);
        RegisteredInstances[lastSoAIndex] = null;

        HandleVersions[Handle.Index]++; // invalidate stale handles
        _freeHandleIndices.Enqueue(Handle.Index);

        Handle = AmbiHandle.Null;
        SoAIndex = INACTIVE;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}

[NoAutoStaticsCleanup]
public abstract class AmbiMonoBehaviour<T> : MonoBehaviour
    where T : AmbiMonoBehaviour<T>
{
    static int _maximumCapacity;
    static T _allocationOwner;
    public const int INACTIVE = -1;

    public static NativeArray<int> HandleToSoA;
    public static NativeArray<int> SoAToHandle;
    public static NativeArray<int> HandleVersions;

    private static Queue<int> _freeHandleIndices;
    private static int _nextHandleIndex;

    public AmbiHandle Handle { get; private set; } = AmbiHandle.Null;
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

    // csharpier-ignore
    protected void InitializeRegistry()
    {
        if (RegisteredInstances != null)
            return;

        _maximumCapacity = AllocatedCapacity;

        if (_maximumCapacity <= 0)
            throw new InvalidOperationException($"{typeof(T).Name} requires a positive maximum capacity.");

        RegisteredInstances = new T[_maximumCapacity];

        HandleToSoA         = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        SoAToHandle         = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        HandleVersions      = new NativeArray<int>(_maximumCapacity, Allocator.Persistent);
        _freeHandleIndices  = new Queue<int>(_maximumCapacity);
        _nextHandleIndex    = 0;

        _allocationOwner    = (T)this;
        AllocateNative();
    }

    protected static void DisposeRegistry()
    {
        if (RegisteredInstances == null)
            return;

        while (ActiveCount != 0)
            RegisteredInstances[ActiveCount - 1].Deregister();

        _allocationOwner.DeallocateNative();

        HandleToSoA.TryDispose();
        SoAToHandle.TryDispose();
        HandleVersions.TryDispose();
        _freeHandleIndices = null;

        _allocationOwner = null;
        RegisteredInstances = null;
        _maximumCapacity = 0;
    }

    // csharpier-ignore
    internal void Register()
    {
        if (IsRegistered)
            return;

        InitializeRegistry();

        if (ActiveCount == _maximumCapacity)
            throw new InvalidOperationException($"Capacity of {_maximumCapacity} exceeded.");

        int handleIndex                    = _freeHandleIndices.Count > 0 ? _freeHandleIndices.Dequeue() : _nextHandleIndex++;
        int version                        = HandleVersions[handleIndex];
        Handle                             = new AmbiHandle { Index = handleIndex, Version = version };

        SoAIndex                           = ActiveCount;
        RegisteredInstances[ActiveCount++] = (T)this;

        HandleToSoA[handleIndex]           = SoAIndex;
        SoAToHandle[SoAIndex]              = handleIndex;

        LoadObjectToArrays();
    }

    // csharpier-ignore
    internal void Deregister()
    {
        if (!IsRegistered)
            return;

        int removedSoAIndex = SoAIndex;
        int lastSoAIndex = --ActiveCount;

        if (removedSoAIndex != lastSoAIndex)
        {
            T movedInstance                      = RegisteredInstances[lastSoAIndex];
            RegisteredInstances[removedSoAIndex] = movedInstance;
            movedInstance.SoAIndex               = removedSoAIndex;

            int movedHandleIndex                 = SoAToHandle[lastSoAIndex];
            HandleToSoA[movedHandleIndex]        = removedSoAIndex;
            SoAToHandle[removedSoAIndex]         = movedHandleIndex;
        }

        RemoveAtSwapBack(removedSoAIndex, lastSoAIndex);
        RegisteredInstances[lastSoAIndex] = null;

        HandleVersions[Handle.Index]++; // invalidate stale handles
        _freeHandleIndices.Enqueue(Handle.Index);

        Handle = AmbiHandle.Null;
        SoAIndex = INACTIVE;

        if (ActiveCount == 0 && !RetainNativeWhenEmpty)
            DisposeRegistry();
    }
}
