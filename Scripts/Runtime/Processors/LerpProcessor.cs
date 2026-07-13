using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal static class LerpProcessor
{
    public static JobHandle ScheduleLerping(JobHandle? appendTo = default)
    {
        var poolController = WyrmPoolController.Instance;
        var activeCount = poolController.ActiveCount;

        var lerpOcclusions = new StatelessLerpOcclusion01Job
        {
            TargetOcclusions01 = poolController.TargetOcclusion01s,
            LerpSpeed = 5f,
            DeltaTime = Time.deltaTime,

            CurrentOcclusions01 = poolController.CurrentOcclusion01,
        };
        JobHandle lerpOcclusionsHandle = lerpOcclusions.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        var LerpPropagationEQs = new StatelessLerpPropagation01Job
        {
            TargetPropagationEQs01 = poolController.TargetPropagationEQ01,
            LerpSpeed = 5f,
            DeltaTime = Time.deltaTime,
            CurrentPropagationEQs01 = poolController.CurrentPropagationEQ01s,
        };
        JobHandle lerpPropagationEQsHandle = LerpPropagationEQs.Schedule(activeCount, 16, dependsOn: appendTo ?? default);

        return JobHandle.CombineDependencies(lerpOcclusionsHandle, lerpPropagationEQsHandle);
    }
}

internal interface ILerpProcessor
{
    JobHandle ScheduleLerping();
}

internal struct StatelessLerpOcclusion01Job : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TargetOcclusions01;
    [ReadOnly] public float LerpSpeed;
    [ReadOnly] public float DeltaTime;

    public NativeArray<float> CurrentOcclusions01;

    public void Execute(int index)
    {
        float current = CurrentOcclusions01[index];
        float target = TargetOcclusions01[index];

        CurrentOcclusions01[index] = math.lerp(
            current,
            target,
            1f - math.exp(-LerpSpeed * DeltaTime)
        );
    }
}

internal struct StatelessLerpPropagation01Job : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> TargetPropagationEQs01;
    [ReadOnly] public float LerpSpeed;
    [ReadOnly] public float DeltaTime;

    public NativeArray<float3> CurrentPropagationEQs01;

    public void Execute(int index)
    {
        float3 current = CurrentPropagationEQs01[index];
        float3 target = TargetPropagationEQs01[index];

        float t = 1f - math.exp(-LerpSpeed * DeltaTime);

        CurrentPropagationEQs01[index] = math.lerp(current, target, t);
    }
}