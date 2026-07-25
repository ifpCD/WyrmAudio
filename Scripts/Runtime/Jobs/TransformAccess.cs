using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

[BurstCompile]
internal struct GatherSourcePositionsJob : IJobParallelForTransform
{
    [WriteOnly]
    public NativeArray<float3> SourcePositions;

    public void Execute(int index, TransformAccess transform) => SourcePositions[index] = transform.position;
}

[BurstCompile]
internal struct ApplySourceTransformsJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<float3> SourcePositions;

    public void Execute(int index, TransformAccess transform) => transform.position = SourcePositions[index];
}
