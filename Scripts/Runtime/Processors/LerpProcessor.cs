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

        float expLerpFactor = GetExpLerpFactor(WyrmAudioSettings.Instance.OcclusionLerpSpeed);

        int activeCount = WyrmBaseSource.ActiveCount;

        // csharpier-ignore
        var lerpOcclusions = new ExponentialFloatLerpJob
        {
            Targets               = WyrmBaseSource.TargetOcclusion01,
            ExponentialLerpFactor = expLerpFactor,

            Currents              = WyrmBaseSource.CurrentOcclusion01,
        };
        JobHandle lerpOcclusionsHandle = lerpOcclusions.Schedule(activeCount, 16, dependency);

        return lerpOcclusionsHandle;

        // csharpier-ignore
        // var lerpPropagationEqs = new ExponentialFloat3LerpJob
        // {
        //     Targets               = WyrmBaseSource.TargetAmbisonicEQ01s,
        //     ExponentialLerpFactor = expLerpFactor,

        //     Currents              = WyrmBaseSource.CurrentAmbisonicEQ01s,
        // };
        // JobHandle lerpPropagationEqsHandle = lerpPropagationEqs.Schedule(activeCount, 16, dependency);

        // return JobHandle.CombineDependencies(lerpOcclusionsHandle, lerpPropagationEqsHandle);
    }
}
