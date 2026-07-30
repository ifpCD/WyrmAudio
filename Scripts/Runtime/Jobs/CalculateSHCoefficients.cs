using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateSHCoefficientsJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> VirtualPositions;

    [ReadOnly]
    public NativeArray<float> DirectionalGains;

    [ReadOnly]
    public NativeArray<float> AmbientGains;

    [ReadOnly]
    public NativeArray<float> HorizontalWidths; // 0 to 1 (0 = point, 1 = wrap around)

    [ReadOnly]
    public NativeArray<float> VerticalWidths; // 0 to 1

    [ReadOnly]
    public NativeReference<float3> ListenerPosition;

    [ReadOnly]
    public int AmbisonicOrder;

    [NativeDisableParallelForRestriction]
    [WriteOnly]
    public NativeArray<float> SHCoeffs;

    public void Execute(int i)
    {
        float3 dir = VirtualPositions[i] - ListenerPosition.Value;
        float dist = math.length(dir);
        float3 u = math.select(new float3(0, 0, 1), dir / dist, dist > 0.001f);

        // Phonon Mapping
        float x = u.z;
        float y = -u.x;
        float z = u.y;

        int numCoeffs = (AmbisonicOrder + 1) * (AmbisonicOrder + 1);
        int offset = i * numCoeffs;

        // Spread Parameters (Convert 0-1 to Radians)
        // We cap spread to prevent numerical issues, 0.01 to PI
        float sigmaH = HorizontalWidths[i] * math.PI;
        float sigmaV = VerticalWidths[i] * math.PI;

        float directionalGain = DirectionalGains[i];

        // Order 0 (Omni / Ambient)
        // Order 0 is unaffected by width/direction, it's the base floor.
        SHCoeffs[offset + 0] = AmbientGains[i] * SH.C0;

        if (AmbisonicOrder == 0)
            return;

        // Precompute smoothing weights per order/degree
        // We use a simplified anisotropic smoothing kernel
        float w_l1_h = math.exp(-(1 * 2 * (sigmaH * sigmaH)) / 2.0f);
        float w_l1_v = math.exp(-(1 * 2 * (sigmaV * sigmaV)) / 2.0f);

        // Order 1
        // m = -1 (Y), m = 0 (Z), m = 1 (X)
        // Scaling X and Z by horizontal width, Y by vertical
        SHCoeffs[offset + 1] = directionalGain * w_l1_h * SH.C1 * y; // Horizontal detail
        SHCoeffs[offset + 2] = directionalGain * w_l1_v * SH.C1 * z; // Vertical detail
        SHCoeffs[offset + 3] = directionalGain * w_l1_h * SH.C1 * x; // Horizontal detail

        if (AmbisonicOrder == 1)
            return;

        // Order 2 Smoothing Weights
        float w_l2_h = math.exp(-(2 * 3 * (sigmaH * sigmaH)) / 2.0f);
        float w_l2_v = math.exp(-(2 * 3 * (sigmaV * sigmaV)) / 2.0f);

        float xx = x * x;
        float yy = y * y;
        float zz = z * z;
        float xy = x * y;
        float yz = y * z;
        float xz = x * z;

        // Higher |m| = higher horizontal frequency
        SHCoeffs[offset + 4] = directionalGain * w_l2_h * SH.C2 * xy;
        SHCoeffs[offset + 5] = directionalGain * w_l2_h * SH.C2 * yz;
        SHCoeffs[offset + 6] = directionalGain * w_l2_v * SH.C3 * (3.0f * zz - 1.0f);
        SHCoeffs[offset + 7] = directionalGain * w_l2_h * SH.C2 * xz;
        SHCoeffs[offset + 8] = directionalGain * w_l2_h * SH.C4 * (xx - yy);

        if (AmbisonicOrder == 2)
            return;

        // Order 3 Smoothing Weights
        float w_l3_h = math.exp(-(3 * 4 * (sigmaH * sigmaH)) / 2.0f);
        float w_l3_v = math.exp(-(3 * 4 * (sigmaV * sigmaV)) / 2.0f);

        SHCoeffs[offset + 9] = directionalGain * w_l3_h * SH.C5 * y * (3.0f * xx - yy);
        SHCoeffs[offset + 10] = directionalGain * w_l3_h * SH.C6 * x * y * z;
        SHCoeffs[offset + 11] = directionalGain * w_l3_h * SH.C7 * y * (5.0f * zz - 1.0f);
        SHCoeffs[offset + 12] = directionalGain * w_l3_v * SH.C8 * z * (5.0f * zz - 3.0f);
        SHCoeffs[offset + 13] = directionalGain * w_l3_h * SH.C7 * x * (5.0f * zz - 1.0f);
        SHCoeffs[offset + 14] = directionalGain * w_l3_h * SH.C9 * z * (xx - yy);
        SHCoeffs[offset + 15] = directionalGain * w_l3_h * SH.C5 * x * (xx - 3.0f * yy);
    }
}
