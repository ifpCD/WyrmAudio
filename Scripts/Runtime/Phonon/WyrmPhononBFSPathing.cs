using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class WyrmPhononCustomAPI
{
    [DllImport("phonon")]
    public static unsafe extern void iplSourceSetCustomPathingBatch(int numSources, IntPtr* sources, float* eqCoeffs, float* shCoeffs, int shOrder);
}

[BurstCompile]
public struct CalculateSHCoefficientsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Directions;
    [ReadOnly] public NativeArray<float> Distances;
    [ReadOnly] public int AmbisonicOrder;


    // because we are doing SHCoeffs[i * numCoeffs + j]
    // unity doesn't like it because we are not accessing the native array by our current index 
    [NativeDisableParallelForRestriction]
    [WriteOnly] public NativeArray<float> SHCoeffs;

    public void Execute(int i)
    {
        float dist = Distances[i];
        float gain = 1.0f / math.max(dist, 1.0f);

        float3 dir = Directions[i];
        if (math.lengthsq(dir) < 0.0001f)
            dir = new float3(0, 0, 1);

        float3 u = math.normalize(dir);

        // Unity = (Ux, Uy, Uz).
        // Phonon = (Ux, Uy, -Uz).
        // Google SH = (-Pz, -Px, Py).
        // so Google SH = (Uz, -Ux, Uy)
        float x = u.z;
        float y = -u.x;
        float z = u.y;

        int numCoeffs = (AmbisonicOrder + 1) * (AmbisonicOrder + 1);
        int offset = i * numCoeffs;

        // Order 0
        SHCoeffs[offset + 0] = gain * 0.28209479f;

        if (AmbisonicOrder >= 1)
        {
            SHCoeffs[offset + 1] = gain * 0.48860251f * y;
            SHCoeffs[offset + 2] = gain * 0.48860251f * z;
            SHCoeffs[offset + 3] = gain * 0.48860251f * x;
        }

        if (AmbisonicOrder >= 2)
        {
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float yz = y * z;
            float xz = x * z;

            SHCoeffs[offset + 4] = gain * 1.0925484f * xy;
            SHCoeffs[offset + 5] = gain * 1.0925484f * yz;
            SHCoeffs[offset + 6] = gain * 0.3153915f * (3.0f * zz - 1.0f);
            SHCoeffs[offset + 7] = gain * 1.0925484f * xz;
            SHCoeffs[offset + 8] = gain * 0.5462742f * (xx - yy);
        }

        if (AmbisonicOrder >= 3)
        {
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;

            SHCoeffs[offset + 9] = gain * 0.5900435f * y * (3.0f * xx - yy);
            SHCoeffs[offset + 10] = gain * 2.8906114f * x * y * z;
            SHCoeffs[offset + 11] = gain * 0.4570457f * y * (5.0f * zz - 1.0f);
            SHCoeffs[offset + 12] = gain * 0.3731763f * z * (5.0f * zz - 3.0f);
            SHCoeffs[offset + 13] = gain * 0.4570457f * x * (5.0f * zz - 1.0f);
            SHCoeffs[offset + 14] = gain * 1.4453057f * z * (xx - yy);
            SHCoeffs[offset + 15] = gain * 0.5900435f * x * (xx - 3.0f * yy);
        }
    }
}