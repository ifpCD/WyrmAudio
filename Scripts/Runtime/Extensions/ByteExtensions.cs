using System;
using System.Runtime.CompilerServices;

public static class ByteExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToBool(this byte value) => Convert.ToBoolean(value);
}
