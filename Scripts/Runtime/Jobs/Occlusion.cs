using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GenerateSourceRaycastCommands : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> SourcePositions;

    [ReadOnly]
    public NativeArray<byte> UseOcclusions;

    [ReadOnly]
    public float3 ListenerPosition;

    [ReadOnly]
    public QueryParameters QueryParameters;

    [WriteOnly]
    public NativeArray<RaycastCommand> RaycastCommands;

    public void Execute(int index)
    {
        if (UseOcclusions[index] == 0)
        {
            RaycastCommands[index] = new RaycastCommand();
            return;
        }

        float3 src = SourcePositions[index];
        float3 dir = ListenerPosition - src;
        float dist = math.length(dir);

        if (dist > 0.001f)
        {
            float3 dirNorm = dir / dist;
            RaycastCommands[index] = new RaycastCommand(src, dirNorm, QueryParameters, dist);
        }
        else
        {
            RaycastCommands[index] = new RaycastCommand();
        }
    }
}

[BurstCompile]
public struct ResolveOcclusionJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<RaycastHit> RaycastHits;

    [ReadOnly]
    public NativeArray<byte> UseOcclusions;

    [WriteOnly]
    public NativeArray<float> SourceOcclusions;

    public void Execute(int index)
    {
        bool occluded = UseOcclusions[index] != 0 && RaycastHits[index].distance > 0f;
        SourceOcclusions[index] = math.select(1f, 0f, occluded);
    }
}
