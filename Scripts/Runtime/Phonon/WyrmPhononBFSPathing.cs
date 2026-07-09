using Unity.Mathematics;
using System.Runtime.InteropServices;
using System;
using Unity.Collections;

public static unsafe class WyrmCustomPathing
{
    [DllImport("phonon")]
    public static extern void iplSourceSetCustomPathing(IntPtr source, float* eqCoeffs, float* shCoeffs);

    // Helper to calculate SH inside a Burst Job
    public static void ProjectToAmbisonics(float3 listenerToPortalDir, int order, float gain, ref NativeArray<float> shCoeffs)
    {
        // Convert Unity coords to Phonon SH coords: (-z, -x, y)
        float3 dir = math.normalize(new float3(-listenerToPortalDir.z, -listenerToPortalDir.x, listenerToPortalDir.y));

        // 1st Order Ambisonics (4 coefficients). 
        // Expand this if you want 2nd (9) or 3rd (16) order.
        shCoeffs[0] = gain * 0.28209479f; // L0_0

        if (order >= 1)
        {
            shCoeffs[1] = gain * -0.48860251f * dir.y; // L1_-1
            shCoeffs[2] = gain * 0.48860251f * dir.z; // L1_0
            shCoeffs[3] = gain * -0.48860251f * dir.x; // L1_1
        }
    }
}