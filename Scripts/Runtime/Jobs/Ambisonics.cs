using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SimpleAmbisonicsGeneration : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<byte> Types;

    [ReadOnly]
    public NativeArray<float3> VirtualPositions;

    [ReadOnly]
    public NativeArray<float3> VirtualDirections;

    [ReadOnly]
    public NativeArray<float> HorizontalBlurs;

    [ReadOnly]
    public NativeArray<float> VerticalBlurs;

    [ReadOnly]
    public NativeArray<float3> EQVolume01s;

    [ReadOnly]
    public float3 ListenerPosition;

    [ReadOnly]
    public int AmbisonicOrder;

    public NativeArray<int> SimpleAmbisonicsToContributorIndices; 

    [NativeDisableParallelForRestriction]
    [WriteOnly]
    public NativeArray<float> AmbisonicOutputsBuffer;

    // iterated over SimpleAmbisonics.ActiveCount
    public void Execute(int index)
    {
        float3 dir;

        if (Types[index] == SimpleAmbisonicType.Positional)
            dir = VirtualPositions[index] - ListenerPosition;
        else
            dir = VirtualDirections[index];

        float3 u = math.normalizesafe(dir, float3.zero);

        // Phonon Mapping
        float x = u.z;
        float y = -u.x;
        float z = u.y;

        int numCoeffs = (AmbisonicOrder + 1) * (AmbisonicOrder + 1);

        int offsetLow = index * numCoeffs * 3;
        int offsetMid = offsetLow + numCoeffs;
        int offsetHigh = offsetMid + numCoeffs;

        float3 eq = PathEQs[index];

        var shCoeffs = SHCoeffs;

        void WriteCoeff(int coefficient, float value)
        {
            shCoeffs[offsetLow + coefficient] = value * eq.x;
            shCoeffs[offsetMid + coefficient] = value * eq.y;
            shCoeffs[offsetHigh + coefficient] = value * eq.z;
        }

        // 0th Order
        WriteCoeff(0, SH.C0);

        // 1st Order
        WriteCoeff(1, SH.C1 * y);
        WriteCoeff(2, SH.C1 * z);
        WriteCoeff(3, SH.C1 * x);

        // 2nd Order
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

        // 3rd Order
        WriteCoeff(9, SH.C5 * y * (3.0f * xx - yy));
        WriteCoeff(10, SH.C6 * x * y * z);
        WriteCoeff(11, SH.C7 * y * (5.0f * zz - 1.0f));
        WriteCoeff(12, SH.C8 * z * (5.0f * zz - 3.0f));
        WriteCoeff(13, SH.C7 * x * (5.0f * zz - 1.0f));
        WriteCoeff(14, SH.C9 * z * (xx - yy));
        WriteCoeff(15, SH.C5 * x * (xx - 3.0f * yy));
    }
}
