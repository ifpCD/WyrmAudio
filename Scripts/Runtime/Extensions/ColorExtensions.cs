using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ColorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithAlpha(this Color color, float alpha) => new(color.r, color.g, color.b, alpha);
}
