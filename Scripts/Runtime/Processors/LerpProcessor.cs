using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class LerpProcessor
{
    [BurstCompile]
    public static float GetExpLerpFactor(float lerpSpeed) => 1f - math.exp(-lerpSpeed * Time.deltaTime);

    public static JobHandle Schedule(JobHandle dependency)
    {
        int activeCount = WyrmBaseSource.ActiveCount;
        if (activeCount == 0)
            return dependency;

        float expLerpFactor = GetExpLerpFactor(5f);

        // csharpier-ignore
        var lerpOcclusions = new StatelessLerpOcclusion01Job
        {
            TargetOcclusions01  = WyrmBaseSource.TargetOcclusion01,
            ExpLerpFactor       = expLerpFactor,

            CurrentOcclusions01 = WyrmBaseSource.CurrentOcclusion01,
        };
        JobHandle lerpOcclusionsHandle = lerpOcclusions.Schedule(activeCount, 16, dependency);

        // csharpier-ignore
        var lerpPropagationEqs = new StatelessLerpPropagation01Job
        {
            TargetPropagationEQs01  = WyrmBaseSource.TargetPropagationEQ01,
            ExpLerpFactor           = expLerpFactor,

            CurrentPropagationEQs01 = WyrmBaseSource.CurrentPropagationEQ01,
        };
        JobHandle lerpPropagationEqsHandle = lerpPropagationEqs.Schedule(activeCount, 16, dependency);

        return JobHandle.CombineDependencies(lerpOcclusionsHandle, lerpPropagationEqsHandle);
    }
}

