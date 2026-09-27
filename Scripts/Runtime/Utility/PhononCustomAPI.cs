using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

// csharpier-ignore
public static class WyrmPhononCustomAPI
{
    // bandOrders: numSources x HC.MAX_AMBISONIC_BANDS, bandCoeffs: numSources x HC.AMBISONIC_BUFFER_LENGTH
    [DllImport("phonon")]
    public static extern unsafe void iplSourceSetAmbisonicFieldBatch(int numSources, IntPtr* sources, int3* bandOrders, float* bandCoeffs);

    [DllImport("phonon")]
    public static extern unsafe void iplSourceSetCustomDirectBatch(int numSources, IntPtr* sources, float3* positions, float* occlusions, float3* transmissions);
}
