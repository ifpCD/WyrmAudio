using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateSHCoefficientsJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> Directions;

    [ReadOnly]
    public NativeArray<float> Distances;

    [ReadOnly]
    public int AmbisonicOrder;

    // because we are doing SHCoeffs[i * numCoeffs + j]
    // unity doesn't like it because we are not accessing the native array by our current index
    [NativeDisableParallelForRestriction]
    [WriteOnly]
    public NativeArray<float> SHCoeffs;

    public void Execute(int i)
    {
        float dist = Distances[i];
        float gain = 1.0f / (1.0f + dist * 0.1f);
        // float gain = math.exp(-dist * 0.15f);
        gain *= 0.1f;

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
        SHCoeffs[offset + 0] = gain * SH.C0;

        if (AmbisonicOrder >= 1)
        {
            SHCoeffs[offset + 1] = gain * SH.C1 * y;
            SHCoeffs[offset + 2] = gain * SH.C1 * z;
            SHCoeffs[offset + 3] = gain * SH.C1 * x;
        }

        if (AmbisonicOrder >= 2)
        {
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float yz = y * z;
            float xz = x * z;

            SHCoeffs[offset + 4] = gain * SH.C2 * xy;
            SHCoeffs[offset + 5] = gain * SH.C2 * yz;
            SHCoeffs[offset + 6] = gain * SH.C3 * (3.0f * zz - 1.0f);
            SHCoeffs[offset + 7] = gain * SH.C2 * xz;
            SHCoeffs[offset + 8] = gain * SH.C4 * (xx - yy);
        }

        if (AmbisonicOrder >= 3)
        {
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;

            SHCoeffs[offset + 9] = gain * SH.C5 * y * (3.0f * xx - yy);
            SHCoeffs[offset + 10] = gain * SH.C6 * x * y * z;
            SHCoeffs[offset + 11] = gain * SH.C7 * y * (5.0f * zz - 1.0f);
            SHCoeffs[offset + 12] = gain * SH.C8 * z * (5.0f * zz - 3.0f);
            SHCoeffs[offset + 13] = gain * SH.C7 * x * (5.0f * zz - 1.0f);
            SHCoeffs[offset + 14] = gain * SH.C9 * z * (xx - yy);
            SHCoeffs[offset + 15] = gain * SH.C5 * x * (xx - 3.0f * yy);
        }
    }
}
