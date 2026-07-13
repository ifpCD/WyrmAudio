using Unity.Burst;
using Unity.Collections;
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

internal struct StatelessLerpOcclusion01Job : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TargetOcclusions01;
    [ReadOnly] public float ExpLerpFactor;

    public NativeArray<float> CurrentOcclusions01;

    public void Execute(int index)
    {
        float current = CurrentOcclusions01[index];
        float target = TargetOcclusions01[index];

        CurrentOcclusions01[index] = math.lerp(current, target, ExpLerpFactor);
    }
}

internal struct StatelessLerpPropagation01Job : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> TargetPropagationEQs01;
    [ReadOnly] public float ExpLerpFactor;

    public NativeArray<float3> CurrentPropagationEQs01;
    const float epsilonSquared = 1e-8f;

    public void Execute(int index)
    {
        float3 current = CurrentPropagationEQs01[index];
        float3 target = TargetPropagationEQs01[index];

        // because we aren't normalizing Path EQ, IIR explodes if we feed it absolute 0
        // so we clamp it
        CurrentPropagationEQs01[index] = math.max(
            math.lerp(current, target, ExpLerpFactor),
            math.EPSILON
        );
    }
}