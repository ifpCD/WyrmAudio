using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

internal static class EffectsMixingProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive)
            return dependency;

        // csharpier-ignore
        var downMixIfVisibleJob = new DownmixPropagationOnVisibilityJob
        {
            TargetOcclusion01s     = WyrmBaseSource.TargetOcclusion01,

            TargetPropagationEQ01s = WyrmBaseSource.TargetAmbisonicEQ01s,
        }.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);

        return downMixIfVisibleJob;
    }
}

[BurstCompile]
internal struct DownmixPropagationOnVisibilityJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> TargetOcclusion01s;

    public NativeArray<float3> TargetPropagationEQ01s;

    public void Execute(int index)
    {
        TargetPropagationEQ01s[index] *= 1f - TargetOcclusion01s[index];
    }
}
