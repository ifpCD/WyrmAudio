using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
internal struct GatherMaskTransformsJob : IJobParallelForTransform
{
    [WriteOnly]
    public NativeArray<float4x4> LocalToWorlds;

    public void Execute(int index, TransformAccess transform)
    {
        LocalToWorlds[index] = transform.localToWorldMatrix;
    }
}

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
        var maskIndex = SampleToMask[sampleIndex];
        var sampleLocalPos = SampleLocalPositions[sampleIndex];

        float4x4 maskLocalToWorld = MaskLocalToWorlds[maskIndex];
        float3 maskWorldPos = maskLocalToWorld.GetPosition();
        float3 maskLossyScale = maskLocalToWorld.GetLossyScale();

        float3 sampleWorldPos = maskWorldPos + sampleLocalPos * maskLossyScale;
        SampleWorldPositions[sampleIndex] = sampleWorldPos;

        if (!SampleIsDiscardable[sampleIndex])
        {
            SampleDiscardCommands[sampleIndex] = default;
            return;
        }

        float3 dir = sampleWorldPos - maskWorldPos;
        float dist = math.length(dir);

        if (dist > 1e-5f)
        {
            float3 dirNorm = dir / dist;
            SampleDiscardCommands[sampleIndex] = new RaycastCommand(maskWorldPos, dirNorm, QueryParameters, dist);
        }
        else
        {
            SampleDiscardCommands[sampleIndex] = default;
        }
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
    public NativeArray<bool> SampleIsDiscarded;

    [WriteOnly]
    public NativeArray<RaycastCommand> SampleOcclusionCommands;

    public void Execute(int sampleIndex)
    {
        bool isDiscarded = SampleDiscardResults[sampleIndex].distance > 0;
        SampleIsDiscarded[sampleIndex] = isDiscarded;

        float3 listenerWorldPos = ListenerPosition.Value;
        float3 sampleWorldPos = SampleWorldPositions[sampleIndex];

        if (!isDiscarded)
        {
            float3 dir = sampleWorldPos - listenerWorldPos;
            float dist = math.length(dir);

            if (dist > 1e-5f)
            {
                float3 dirNorm = dir / dist;
                SampleOcclusionCommands[sampleIndex] = new RaycastCommand(listenerWorldPos, dirNorm, QueryParameters, dist);
            }
            else
            {
                SampleOcclusionCommands[sampleIndex] = default;
            }
        }
        else
        {
            SampleOcclusionCommands[sampleIndex] = default;
        }
    }
}

[BurstCompile]
public struct ResolveMaskOcclusionValueJob : IJob
{
    [ReadOnly]
    public NativeArray<RaycastHit> SampleOcclusionResults;

    [ReadOnly]
    public NativeArray<bool> SampleIsDiscarded;

    [ReadOnly]
    public NativeArray<int> SampleToMask;

    [ReadOnly]
    public NativeArray<float> SampleWeights;

    [ReadOnly]
    public int SampleActiveCount;

    [ReadOnly]
    public int MaskActiveCount;

    public NativeArray<float> MaskTargetOcclusion01s;

    public NativeArray<float> MaskTotalSampleWeight;
    public NativeArray<float> MaskOccludedSampleWeight;

    [WriteOnly]
    public NativeArray<bool> SampleIsOccluded;

    public void Execute()
    {
        MaskTotalSampleWeight.SubFill(0, MaskActiveCount);
        MaskOccludedSampleWeight.SubFill(0, MaskActiveCount);

        for (int sampleIndex = 0; sampleIndex < SampleActiveCount; sampleIndex++)
        {
            int maskIndex = SampleToMask[sampleIndex];
            if (maskIndex < 0 || maskIndex >= MaskActiveCount)
                continue;

            bool isDiscarded = SampleIsDiscarded[sampleIndex];

            if (isDiscarded)
                continue;

            float weight = SampleWeights[sampleIndex];
            MaskTotalSampleWeight[maskIndex] += weight;

            bool isOccluded = SampleOcclusionResults[sampleIndex].distance > 0f;
            SampleIsOccluded[sampleIndex] = isOccluded;

            if (isOccluded)
            {
                MaskOccludedSampleWeight[maskIndex] += weight;
            }
        }

        for (int maskIndex = 0; maskIndex < MaskActiveCount; maskIndex++)
        {
            float totalWeight = MaskTotalSampleWeight[maskIndex];
            MaskTargetOcclusion01s[maskIndex] = totalWeight > 0f ? math.saturate(MaskOccludedSampleWeight[maskIndex] / totalWeight) : 1f;
        }
    }
}

[BurstCompile]
public struct ApplyMaskOcclusionToSourceJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> SourceToMaskIndex;

    [ReadOnly]
    public NativeArray<bool> UseOcclusions;

    [ReadOnly]
    public NativeArray<float> MaskTargetOcclusion01s;

    public NativeArray<float> TargetOcclusion01;

    public void Execute(int sourceIndex)
    {
        if (!UseOcclusions[sourceIndex])
        {
            TargetOcclusion01[sourceIndex] = 0f;
            return;
        }

        // mask is guaranteed if the source is registered
        int maskIndex = SourceToMaskIndex[sourceIndex];
        TargetOcclusion01[sourceIndex] = 1f - MaskTargetOcclusion01s[maskIndex];
    }
}
