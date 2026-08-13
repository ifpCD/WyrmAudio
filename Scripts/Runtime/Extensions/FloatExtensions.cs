using System.Runtime.CompilerServices;
using UnityEngine;

public static class FloatExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float WithVariation(this float value, float amount = 0.05f) => value + Random.Range(-amount, amount);
}