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

        var downMixIfVisible = new StatelessPropagationVisibilityDownMixingJob
        {
            TargetOcclusion01s = poolController.TargetOcclusion01,
            TargetPropagationEQ01s = poolController.TargetPropagationEQ01,
        };
        JobHandle downMixIfVisibleHandle = downMixIfVisible.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        return downMixIfVisibleHandle;
    }
}

internal struct StatelessPropagationVisibilityDownMixingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TargetOcclusion01s;

    public NativeArray<float3> TargetPropagationEQ01s;

    public void Execute(int index)
    {
        TargetPropagationEQ01s[index] *= 1f - TargetOcclusion01s[index];
        TargetPropagationEQ01s[index] *= 3f;
    }
}