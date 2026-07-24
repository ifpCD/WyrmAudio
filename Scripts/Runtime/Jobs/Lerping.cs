using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
internal struct StatelessLerpOcclusion01Job : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> TargetOcclusions01;

    [ReadOnly]
    public float ExpLerpFactor;

    public NativeArray<float> CurrentOcclusions01;

    public void Execute(int index)
    {
        float current = CurrentOcclusions01[index];
        float target = TargetOcclusions01[index];

        CurrentOcclusions01[index] = math.lerp(current, target, ExpLerpFactor);
    }
}

[BurstCompile]
internal struct StatelessLerpPropagation01Job : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> TargetPropagationEQs01;

    [ReadOnly]
    public float ExpLerpFactor;

    public NativeArray<float3> CurrentPropagationEQs01;

    public void Execute(int index)
    {
        float3 current = CurrentPropagationEQs01[index];
        float3 target = TargetPropagationEQs01[index];

        // because we aren't normalizing Path EQ, Phonon's IIR explodes if we feed it absolute 0
        // so we clamp it
        CurrentPropagationEQs01[index] = math.max(math.lerp(current, target, ExpLerpFactor), math.EPSILON);
    }
}
