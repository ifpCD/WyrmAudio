using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class LerpProcessor
{
    [BurstCompile]
    public static float GetExpLerpFactor(float LerpSpeed) => 1f - math.exp(-LerpSpeed * Time.deltaTime);

    public static JobHandle ScheduleLerping(JobHandle? appendTo = default)
    {
        var poolController = WyrmPoolController.Instance;
        var activeCount = poolController.ActiveCount;
        float ExpLerpFactor = GetExpLerpFactor(5f);

        var lerpOcclusions = new StatelessLerpOcclusion01Job
        {
            TargetOcclusions01 = poolController.TargetOcclusion01,
            ExpLerpFactor = ExpLerpFactor,

            CurrentOcclusions01 = poolController.CurrentOcclusion01,
        };
        JobHandle lerpOcclusionsHandle = lerpOcclusions.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        var LerpPropagationEQs = new StatelessLerpPropagation01Job
        {
            TargetPropagationEQs01 = poolController.TargetPropagationEQ01,
            ExpLerpFactor = ExpLerpFactor,

            CurrentPropagationEQs01 = poolController.CurrentPropagationEQ01s,
        };
        JobHandle lerpPropagationEQsHandle = LerpPropagationEQs.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        return JobHandle.CombineDependencies(lerpOcclusionsHandle, lerpPropagationEQsHandle);
    }
}

