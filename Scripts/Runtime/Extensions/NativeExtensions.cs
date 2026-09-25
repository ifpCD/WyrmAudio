using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public static class NativeExtensions
{
    public static void TryDispose<T>(ref this NativeArray<T> array)
        where T : struct
    {
        if (array.IsCreated)
            array.Dispose();
    }

    public static void TryDispose<T>(ref this NativeReference<T> nativeReference)
        where T : unmanaged
    {
        if (nativeReference.IsCreated)
            nativeReference.Dispose();
    }

    public static void TryDispose(ref this TransformAccessArray array)
    {
        if (array.isCreated)
            array.Dispose();
    }

    public static T GetOrDefault<T>(this NativeArray<T> array, int index)
        where T : struct
    {
        // if (index == -1 || index < 0 || index >= array.Length)
        if (index == -1)
            return default;

        return array[index];
    }

    public static Transform GetOrDefault(this TransformAccessArray array, int index)
    {
        // if (index == -1 || index < 0 || index >= array.Length)
        if (index == -1)
            return default;

        return array[index];
    }

    public static void SetIfEnabled(this TransformAccessArray array, in Transform transform, int index)
    {
        // if (index == -1 || index < 0 || index >= array.Length)
        if (index == -1)
            return;

        array[index] = transform;
    }

    public static void SetIfEnabled<T>(this NativeArray<T> array, in T value, int index)
        where T : struct
    {
        // if (index == -1 || index < 0 || index >= array.Length)
        if (index == -1)
            return;

        array[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill<T>(ref this NativeArray<T> array, T value)
        where T : struct
    {
        for (int i = 0; i < array.Length; i++)
            array[i] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubFill<T>(ref this NativeArray<T> array, T value, int length)
        where T : struct
    {
        for (int i = 0; i < length; i++)
            array[i] = value;
    }
}
