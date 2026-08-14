using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

[BurstCompile]
public struct GenerateDiscardCommands : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> SampleToMask;

    [ReadOnly]
    public NativeArray<float3> SampleLocalPositions;

    [ReadOnly]
    public NativeArray<float4x4> MaskLocalToWorlds;

    [ReadOnly]
    public NativeArray<bool> SampleIsDiscardable;

    [ReadOnly]
    public QueryParameters QueryParameters;

    [WriteOnly]
    public NativeArray<float3> SampleWorldPositions;

    [WriteOnly]
    public NativeArray<RaycastCommand> SampleDiscardCommands;

    public void Execute(int sampleIndex)
    {
        if (!SampleIsDiscardable[sampleIndex])
        {
            SampleDiscardCommands[sampleIndex] = default;
            return;
        }

        var maskIndex = SampleToMask[sampleIndex];

        var sampleLocalPos = SampleLocalPositions[sampleIndex];

        float4x4 maskLocalToWorld = MaskLocalToWorlds[maskIndex];
        float3 maskWorldPos = maskLocalToWorld.GetPosition();
        float3 maskLossyScale = maskLocalToWorld.GetLossyScale();

        float3 sampleWorldPos = maskWorldPos + sampleLocalPos * maskLossyScale;

        SampleWorldPositions[sampleIndex] = sampleWorldPos;

        float3 dir = sampleWorldPos - maskWorldPos;
        float dist = math.length(dir);
        float3 dirNorm = dir / dist;

        SampleDiscardCommands[sampleIndex] = new RaycastCommand(maskWorldPos, dirNorm, QueryParameters, dist);
    }
}

[BurstCompile]
public struct GenerateOcclusionCommands : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> SampleWorldPositions;

    [ReadOnly]
    public NativeReference<float3> ListenerPosition;

    [ReadOnly]
    public QueryParameters QueryParameters;

    [ReadOnly]
    public NativeArray<RaycastHit> SampleDiscardResults;

    [WriteOnly]
    public NativeArray<bool> SampleIsDiscarded; // for visualization

    [WriteOnly]
    public NativeArray<RaycastCommand> SampleOcclusionCommands;

    public void Execute(int sampleIndex)
    {
        bool isDiscarded = SampleDiscardResults[sampleIndex].distance > 0f;

        SampleIsDiscarded[sampleIndex] = isDiscarded;

        float3 listenerWorldPos = ListenerPosition.Value;
        float3 sampleWorldPos = SampleWorldPositions[sampleIndex];

        SampleWorldPositions[sampleIndex] = sampleWorldPos;

        float3 dir = sampleWorldPos - listenerWorldPos;
        float dist = math.length(dir);
        float3 dirNorm = dir / dist;

        if (!isDiscarded)
        {
            SampleOcclusionCommands[sampleIndex] = new RaycastCommand(listenerWorldPos, dirNorm, QueryParameters, dist);
        }
        else
        {
            SampleOcclusionCommands[sampleIndex] = default;
        }
    }
}

[BurstCompile]
public struct ResolveMaskOcclusionValue : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<RaycastResult> OcclusionResults;

    [ReadOnly]
    public NativeArray<int> SampleToMask;

    [ReadOnly]
    public NativeArray<float> SampleWeights;

    [ReadOnly]
    public NativeArray<int> MaskSampleCounts;

    [ReadOnly]
    public NativeArray<RaycastHit> SampleOcclusionResults;

    [WriteOnly]
    public NativeArray<bool> SampleIsOccluded;

    public NativeArray<float> MaskTargetOcclusion01s;

    public void Execute(int sampleIndex)
    {
        bool isOccluded = SampleOcclusionResults[sampleIndex].distance > 0f;

        SampleIsOccluded[sampleIndex] = isOccluded;

        // factor in the total non-discarded count of samples
        float sampleOccContributionToMask = SampleWeights[sampleIndex];

        int maskIndex = SampleToMask[sampleIndex];
        MaskTargetOcclusion01s[maskIndex] += sampleOccContributionToMask;
    }
}