using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class WyrmPhononCustomAPI
{
    [DllImport("phonon")]
    public static unsafe extern void iplSourceSetCustomPathingBatch(
        int numSources, IntPtr* sources, float* eqCoeffs, float* shCoeffs, int shOrder);

    [DllImport("phonon")]
    public static unsafe extern void iplSourceSetCustomDirectBatch(
        int numSources, IntPtr* sources, float3* positions, float* occlusions, float3* transmissions);
}