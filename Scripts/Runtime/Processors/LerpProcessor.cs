using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class LerpProcessor
{
    // [BurstCompile]
    public static float GetExpLerpFactor(float lerpSpeed) => 1f - math.exp(-lerpSpeed * Time.deltaTime);

    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive)
            return dependency;

        float expLerpFactor = GetExpLerpFactor(5f);
        
        int activeCount = WyrmBaseSource.ActiveCount;

        // csharpier-ignore
        var lerpOcclusions = new StatelessLerpOcclusion01Job
        {
            TargetOcclusions01  = WyrmBaseSource.TargetOcclusion01,
            ExpLerpFactor       = expLerpFactor,

            CurrentOcclusions01 = WyrmBaseSource.CurrentOcclusion01,
        };
        JobHandle lerpOcclusionsHandle = lerpOcclusions.Schedule(activeCount, 16, dependency);

        return lerpOcclusionsHandle;

        // csharpier-ignore
        var lerpPropagationEqs = new StatelessLerpPropagation01Job
        {
            TargetPropagationEQs01  = WyrmBaseSource.TargetAmbisonicEQ01s,
            ExpLerpFactor           = expLerpFactor,

            CurrentPropagationEQs01 = WyrmBaseSource.CurrentTotalAmbisonicEQ01s,
        };
        JobHandle lerpPropagationEqsHandle = lerpPropagationEqs.Schedule(activeCount, 16, dependency);

        return JobHandle.CombineDependencies(lerpOcclusionsHandle, lerpPropagationEqsHandle);
    }
}

