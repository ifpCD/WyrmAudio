using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GenerateDiscardCommands : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> MaskSampleCounts;

    [ReadOnly]
    public NativeArray<int> MaskRaycastOffsets;

    [ReadOnly]
    public NativeArray<float4x4> MaskLocalToWorlds;

    [ReadOnly]
    public NativeArray<float3> SampleLocalPositions;

    [ReadOnly]
    public NativeArray<bool> SampleIsDiscardable;

    [ReadOnly]
    public QueryParameters QueryParameters;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<float3> SampleWorldPositions;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<RaycastCommand> RaycastCommands;

    public void Execute(int maskIndex)
    {
        int count = MaskSampleCounts[maskIndex];
        int storageStart = OcclusionSamples.Start(maskIndex);
        int denseStart = MaskRaycastOffsets[maskIndex];

        float4x4 maskLocalToWorld = MaskLocalToWorlds[maskIndex];
        float3 maskWorldPos = maskLocalToWorld.GetPosition();
        float3 maskLossyScale = maskLocalToWorld.GetLossyScale();

        for (int i = 0; i < count; i++)
        {
            int storageIdx = storageStart + i;
            int denseIdx = denseStart + i;

            float3 sampleWorldPos = maskWorldPos + SampleLocalPositions[storageIdx] * maskLossyScale;
            SampleWorldPositions[storageIdx] = sampleWorldPos;

            if (!SampleIsDiscardable[storageIdx])
            {
                RaycastCommands[denseIdx] = default;
                continue;
            }

            float3 dir = sampleWorldPos - maskWorldPos;
            float dist = math.length(dir);

            RaycastCommands[denseIdx] = dist > 1e-5f ? new RaycastCommand(maskWorldPos, dir / dist, QueryParameters, dist) : default;
        }
    }
}

[BurstCompile]
public struct GenerateOcclusionCommands : IJobParallelFor
{
    [ReadOnly]
    public NativeReference<float3> ListenerPosition;

    [ReadOnly]
    public NativeArray<int> MaskSampleCounts;

    [ReadOnly]
    public NativeArray<int> MaskRaycastOffsets;

    [ReadOnly]
    public NativeArray<float3> SampleWorldPositions;

    [ReadOnly]
    public NativeArray<RaycastHit> SampleDiscardResults;

    [ReadOnly]
    public QueryParameters QueryParameters;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<bool> SampleIsDiscarded;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<RaycastCommand> SampleOcclusionCommands;

    public void Execute(int maskIndex)
    {
        int sampleCount = MaskSampleCounts[maskIndex];

        int storageStart = OcclusionSamples.Start(maskIndex);
        int denseStart = MaskRaycastOffsets[maskIndex];

        float3 listenerWorldPos = ListenerPosition.Value;

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            int storageIndex = storageStart + sampleIndex;
            int denseIndex = denseStart + sampleIndex;

            bool isDiscarded = SampleDiscardResults[denseIndex].distance > 0;

            SampleIsDiscarded[storageIndex] = isDiscarded;

            if (isDiscarded)
            {
                SampleOcclusionCommands[denseIndex] = default;
                continue;
            }

            float3 sampleWorldPos = SampleWorldPositions[storageIndex];
            float3 dir = sampleWorldPos - listenerWorldPos;
            float dist = math.length(dir);

            SampleOcclusionCommands[denseIndex] = dist > 1e-5f ? new RaycastCommand(listenerWorldPos, dir / dist, QueryParameters, dist) : default;
        }
    }
}

[BurstCompile]
public struct ResolveMaskOcclusionValueJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> MaskSampleCounts;

    [ReadOnly]
    public NativeArray<int> MaskRaycastOffsets;

    [ReadOnly]
    public NativeArray<RaycastHit> RaycastResults;

    [ReadOnly]
    public NativeArray<bool> SampleIsDiscarded;

    [ReadOnly]
    public NativeArray<float> SampleWeights;

    [WriteOnly]
    public NativeArray<float> MaskTargetOcclusions;

    [WriteOnly, NativeDisableParallelForRestriction]
    public NativeArray<bool> SampleIsOccluded;

    public void Execute(int maskIndex)
    {
        int sampleCount = MaskSampleCounts[maskIndex];

        int storageStart = OcclusionSamples.Start(maskIndex);
        int denseStart = MaskRaycastOffsets[maskIndex];

        float totalWeight = 0f;
        float occludedWeight = 0f;

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            int storageIndex = storageStart + sampleIndex;
            int denseIndex = denseStart + sampleIndex;

            if (SampleIsDiscarded[storageIndex])
                continue;

            float weight = SampleWeights[storageIndex];
            totalWeight += weight;

            bool occluded = RaycastResults[denseIndex].distance > 0f;
            SampleIsOccluded[storageIndex] = occluded;

            if (occluded)
                occludedWeight += weight;
        }

        MaskTargetOcclusions[maskIndex] = totalWeight > 0f ? math.saturate(occludedWeight / totalWeight) : 1f;
    }
}