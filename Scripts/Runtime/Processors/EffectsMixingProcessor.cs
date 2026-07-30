using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class EffectsMixingProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive)
            return dependency;

        // csharpier-ignore
        var downMixIfVisible = new DownmixPropagationOnVisibilityJob
        {
            TargetOcclusion01s     = WyrmBaseSource.TargetOcclusion01,
            
            TargetPropagationEQ01s = WyrmBaseSource.TargetAmbisonicEQ01s,
        };
        return downMixIfVisible.Schedule(WyrmBaseSource.ActiveCount, 16, dependency);
    }
}

[BurstCompile]
internal struct DownmixPropagationOnVisibilityJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TargetOcclusion01s;

    public NativeArray<float3> TargetPropagationEQ01s;

    public void Execute(int index)
    {
        TargetPropagationEQ01s[index] *= 1f - TargetOcclusion01s[index];
    }
}
