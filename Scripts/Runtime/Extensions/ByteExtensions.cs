using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public static class ByteExtensions
{
    public static bool ToBool(this byte value) => Convert.ToBoolean(value);
}
