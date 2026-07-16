using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GenerateSourceRaycastCommands : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> SourcePositions;
    [ReadOnly] public float3 ListenerPosition;
    [ReadOnly] public int LayerMask;

    [WriteOnly] public NativeArray<RaycastCommand> RaycastCommands;

    public void Execute(int index)
    {
        float3 src = SourcePositions[index];
        float3 dir = ListenerPosition - src;
        float dist = math.length(dir);

        if (dist > 0.001f)
        {
            float3 dirNorm = dir / dist;
            RaycastCommands[index] = new RaycastCommand(src, dirNorm, new QueryParameters(LayerMask, false, QueryTriggerInteraction.Ignore), dist);
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
    [ReadOnly] public NativeArray<RaycastHit> RaycastHits;

    [WriteOnly] public NativeArray<float> SourceOcclusions;

    public void Execute(int index)
    {
        SourceOcclusions[index] = RaycastHits[index].distance > 0f ? 0f : 1f;
    }
}