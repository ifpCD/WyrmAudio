using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
#pragma warning disable UNT0022 // Burst TransformAccess doesn't cross boundary

[BurstCompile]
internal struct GatherSourcePositionsJob : IJobParallelForTransform
{
    [WriteOnly]
    public NativeArray<float3> Positions;

    [WriteOnly]
    public NativeArray<quaternion> Rotations;

    [WriteOnly]
    public NativeArray<float4x4> LocalToWorlds;

    public void Execute(int index, TransformAccess transform)
    {
        Positions[index] = transform.position;
        Rotations[index] = transform.rotation;
        LocalToWorlds[index] = transform.localToWorldMatrix;
    }
}

[BurstCompile]
internal struct ApplySourceTransformsJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<float3> Positions;

    [ReadOnly]
    public NativeArray<quaternion> Rotations;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position = Positions[index];
        transform.rotation = Rotations[index];
    }
}
