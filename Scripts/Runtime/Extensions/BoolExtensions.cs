using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public static class BoolExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToByte(this bool value) => Convert.ToByte(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToFloat(this bool value) => value ? 1f : 0f;
}
