using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
internal struct ExponentialFloatLerpJob : IJobParallelFor
{
    [ReadOnly]
    public float ExponentialLerpFactor;

    [ReadOnly]
    public NativeArray<float> Targets;

    public NativeArray<float> Currents;

    public void Execute(int index)
    {
        float current = Currents[index];
        float target = Targets[index];

        Currents[index] = math.lerp(current, target, ExponentialLerpFactor);
    }
}

[BurstCompile]
internal struct ExponentialFloat3LerpJob : IJobParallelFor
{
    [ReadOnly]
    public float ExpLerpFactor;

    [ReadOnly]
    public NativeArray<float3> Targets;

    public NativeArray<float3> Currents;

    public void Execute(int index)
    {
        float3 current = Currents[index];
        float3 target = Targets[index];

        float3 lerpValue = math.lerp(current, target, ExpLerpFactor);

        // Because we aren't normalizing Path EQ - Phonon's IIR explodes if we feed it absolute 0
        Currents[index] = math.max(lerpValue, math.EPSILON);
    }
}
