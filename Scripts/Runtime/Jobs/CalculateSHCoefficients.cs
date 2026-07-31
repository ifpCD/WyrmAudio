using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateDirectionalSHCoefficientJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> VirtualPositions;

    [ReadOnly]
    public NativeArray<float3> PathEQs;

    [ReadOnly]
    public NativeReference<float3> ListenerPosition;

    [ReadOnly]
    public int AmbisonicOrder;

    [NativeDisableParallelForRestriction]
    [WriteOnly]
    public NativeArray<float> SHCoeffs;

    // todo: index should be WyrmBaseSource.ActiveCount * 48 instead
    public void Execute(int i)
    {
        float3 dir = VirtualPositions[i] - ListenerPosition.Value;
        float3 u = math.normalizesafe(dir, float3.zero);

        // Phonon Mapping
        float x = u.z;
        float y = -u.x;
        float z = u.y;

        int numCoeffs = (AmbisonicOrder + 1) * (AmbisonicOrder + 1);

        int offsetLow = i * numCoeffs * 3;
        int offsetMid = offsetLow + numCoeffs;
        int offsetHigh = offsetMid + numCoeffs;

        float3 eq = PathEQs[i];

        var shCoeffs = SHCoeffs;

        void WriteCoeff(int coefficient, float value)
        {
            shCoeffs[offsetLow + coefficient] = value * eq.x;
            shCoeffs[offsetMid + coefficient] = value * eq.y;
            shCoeffs[offsetHigh + coefficient] = value * eq.z;
        }

        WriteCoeff(0, SH.C0);

        if (AmbisonicOrder == 0)
            return;

        WriteCoeff(1, SH.C1 * y);
        WriteCoeff(2, SH.C1 * z);
        WriteCoeff(3, SH.C1 * x);

        if (AmbisonicOrder == 1)
            return;

        float xx = x * x;
        float yy = y * y;
        float zz = z * z;
        float xy = x * y;
        float yz = y * z;
        float xz = x * z;

        WriteCoeff(4, SH.C2 * xy);
        WriteCoeff(5, SH.C2 * yz);
        WriteCoeff(6, SH.C3 * (3.0f * zz - 1.0f));
        WriteCoeff(7, SH.C2 * xz);
        WriteCoeff(8, SH.C4 * (xx - yy));

        if (AmbisonicOrder == 2)
            return;

        WriteCoeff(9, SH.C5 * y * (3.0f * xx - yy));
        WriteCoeff(10, SH.C6 * x * y * z);
        WriteCoeff(11, SH.C7 * y * (5.0f * zz - 1.0f));
        WriteCoeff(12, SH.C8 * z * (5.0f * zz - 3.0f));
        WriteCoeff(13, SH.C7 * x * (5.0f * zz - 1.0f));
        WriteCoeff(14, SH.C9 * z * (xx - yy));
        WriteCoeff(15, SH.C5 * x * (xx - 3.0f * yy));
    }
}
