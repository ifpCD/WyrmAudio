using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class WyrmSourceTransformProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int activeCount = WyrmBaseSource.ActiveCount;
        if (activeCount == 0)
            return dependency;

        var downMixIfVisible = new DownmixPropagationOnVisibilityJob
        {
            TargetOcclusion01s = WyrmBaseSource.TargetOcclusion01,
            TargetPropagationEQ01s = WyrmBaseSource.TargetPropagationEQ01,
        };
        return downMixIfVisible.Schedule(activeCount, 16, dependency);
    }
}

