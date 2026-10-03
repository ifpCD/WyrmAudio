using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ApplyMaskOcclusionToSourceJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<AmbiHandle> SourceMaskHandles;

    [ReadOnly]
    public NativeArray<int> MaskHandleToSoA;

    [ReadOnly]
    public NativeArray<int> MaskVersions;

    [ReadOnly]
    public NativeArray<float> MaskTargetOcclusions;

    public NativeArray<float> TargetOcclusion01;

    public void Execute(int sourceIndex)
    {
        AmbiHandle handle = SourceMaskHandles[sourceIndex];

        if (handle.IsNull || MaskVersions[handle.Index] != handle.Version)
        {
            TargetOcclusion01[sourceIndex] = 0f;
            return;
        }

        int maskSoAIndex = MaskHandleToSoA[handle.Index];
        TargetOcclusion01[sourceIndex] = 1f - MaskTargetOcclusions[maskSoAIndex];
    }
}

[BurstCompile]
internal unsafe struct LoadAmbisonicOutputsToSourcesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<AmbiHandle> SourceGeneratorHandles;

    [ReadOnly]
    public NativeArray<float3> SourceBandGains;

    [ReadOnly]
    public NativeArray<int> GeneratorHandleToSoA;

    [ReadOnly]
    public NativeArray<int> GeneratorVersions;

    [ReadOnly]
    public NativeArray<int3> GeneratorBandOrders;

    [ReadOnly]
    public NativeArray<float> GeneratorOutputs;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float> SourceOutputs;

    [WriteOnly]
    public NativeArray<int3> SourceBandOrders;

    public void Execute(int source)
    {
        float* target = AmbisonicBuffer.Get(SourceOutputs, source);
        AmbiHandle handle = SourceGeneratorHandles[source];

        if (handle.IsNull || GeneratorVersions[handle.Index] != handle.Version)
        {
            AmbisonicBuffer.Clear(target);
            SourceBandOrders[source] = int3.zero;
            return;
        }

        int generator = GeneratorHandleToSoA[handle.Index];
        float* field = AmbisonicBuffer.GetReadOnly(GeneratorOutputs, generator);
        float3 gains = SourceBandGains[source];

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            float gain = gains[band];
            float* bandField = AmbisonicBuffer.GetBand(field, band);
            float* bandTarget = AmbisonicBuffer.GetBand(target, band);

            for (int channel = 0; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
                bandTarget[channel] = bandField[channel] * gain;
        }

        SourceBandOrders[source] = GeneratorBandOrders[generator];
    }
}

[BurstCompile]
internal unsafe struct ClearSourceAmbisonicsJob : IJob
{
    public int SourceCount;

    [WriteOnly]
    public NativeArray<float> SourceOutputs;

    [WriteOnly]
    public NativeArray<int3> SourceBandOrders;

    public void Execute()
    {
        AmbisonicBuffer.Clear(SourceOutputs, 0, SourceCount);
        UnsafeUtility.MemClear(SourceBandOrders.GetUnsafePtr(), (long)SourceCount * sizeof(int3));
    }
}
