using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public static class NativeArrayExtensions
{
    public static void TryDispose<T>(ref this NativeArray<T> nativeArray)
        where T : struct
    {
        if (nativeArray.IsCreated)
            nativeArray.Dispose();
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

    public static void SetIfEnabled(this TransformAccessArray array, Transform transform, int index)
    {
        // if (index == -1 || index < 0 || index >= array.Length)
        if (index == -1)
            return;
            
        array[index] = transform;
    }
}
