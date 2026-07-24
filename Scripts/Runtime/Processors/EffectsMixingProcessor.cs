using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class EffectsMixingProcessor
{
    public static JobHandle ScheduleMixing(JobHandle? appendTo = default)
    {
        var poolController = WyrmPoolController.Instance;
        var activeCount = poolController.ActiveCount;

        var downMixIfVisible = new DownmixPropagationOnVisibilityJob
        {
            TargetOcclusion01s = poolController.TargetOcclusion01,
            TargetPropagationEQ01s = poolController.TargetPropagationEQ01,
        };
        JobHandle downMixIfVisibleHandle = downMixIfVisible.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        return downMixIfVisibleHandle;
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