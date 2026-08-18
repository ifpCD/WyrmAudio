using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int maskActiveCount = WyrmOcclusionMask.ActiveCount;
        int sampleActiveCount = WyrmOcclusionSample.ActiveCount;
        int sourceActiveCount = WyrmBaseSource.ActiveCount;

        if (sourceActiveCount == 0 || WyrmListener.CompletelyInactive || maskActiveCount == 0 || sampleActiveCount == 0)
            return dependency;

        var CommandsSubBuffer = WyrmOcclusionSample.RaycastCommandBuffer.GetSubArray(0, sampleActiveCount);
        var ResultsSubBuffer = WyrmOcclusionSample.RaycastResultBuffer.GetSubArray(0, sampleActiveCount);

        var layerMask = WyrmAudioSettings.Instance.OcclusionMask;
        var queryParameters = new QueryParameters(layerMask, false, QueryTriggerInteraction.Ignore);

        var gatherMasksJob = new GatherMaskTransformsJob { LocalToWorlds = WyrmOcclusionMask.LocalToWorlds };
        JobHandle gatherMasksHandle = gatherMasksJob.Schedule(WyrmOcclusionMask.MaskTransforms, dependency);

        // csharpier-ignore
        var genDiscardCommandsJob = new GenerateDiscardCommands
        {
            SampleToMask          = WyrmOcclusionSample.SampleToMask,
            SampleLocalPositions  = WyrmOcclusionSample.InputLocalPositions,
            MaskLocalToWorlds     = WyrmOcclusionMask.LocalToWorlds,
            SampleIsDiscardable   = WyrmOcclusionSample.InputDiscardable,
            QueryParameters       = queryParameters,

            SampleWorldPositions  = WyrmOcclusionSample.WorldPositions,
            SampleDiscardCommands = CommandsSubBuffer,
        };
        JobHandle genDiscardCommandsHandle = genDiscardCommandsJob.Schedule(sampleActiveCount, 16, gatherMasksHandle);

        JobHandle discardResultsHandle = RaycastCommand.ScheduleBatch(CommandsSubBuffer, ResultsSubBuffer, 16, genDiscardCommandsHandle);

        // csharpier-ignore
        var genOcclusionCommandsJob = new GenerateOcclusionCommands
        {
            SampleWorldPositions    = WyrmOcclusionSample.WorldPositions,
            ListenerPosition        = WyrmListener.ListenerPosition,
            QueryParameters         = queryParameters,
            SampleDiscardResults    = ResultsSubBuffer,

            SampleOcclusionCommands = CommandsSubBuffer,
            SampleIsDiscarded       = WyrmOcclusionSample.IsDiscarded,
        };
        JobHandle genOcclusionCommandsHandle = genOcclusionCommandsJob.Schedule(sampleActiveCount, 16, discardResultsHandle);

        JobHandle occlusionResultHandle = RaycastCommand.ScheduleBatch(CommandsSubBuffer, ResultsSubBuffer, 16, genOcclusionCommandsHandle);

        // csharpier-ignore
        var resolveJob = new ResolveMaskOcclusionValueJob
        {
            SampleOcclusionResults   = ResultsSubBuffer,
            SampleIsDiscarded        = WyrmOcclusionSample.IsDiscarded,
            SampleToMask             = WyrmOcclusionSample.SampleToMask,
            SampleWeights            = WyrmOcclusionSample.InputWeights,
            SampleActiveCount        = sampleActiveCount,
            MaskActiveCount          = maskActiveCount,
            MaskTotalSampleWeight    = WyrmOcclusionMask.TotalSampleWeight,
            MaskOccludedSampleWeight = WyrmOcclusionMask.OccludedSampleWeight,

            MaskTargetOcclusion01s   = WyrmOcclusionMask.TargetOcclusionValue01s,
            SampleIsOccluded         = WyrmOcclusionSample.IsOccluded,
        };
        JobHandle resolveHandle = resolveJob.Schedule(occlusionResultHandle);

        // csharpier-ignore
        var applyJob = new ApplyMaskOcclusionToSourceJob
        {
            SourceToMaskIndex      = WyrmBaseSource.OcclusionMaskIndices,
            UseOcclusions          = WyrmBaseSource.UseOcclusions,
            MaskTargetOcclusion01s = WyrmOcclusionMask.TargetOcclusionValue01s,

            TargetOcclusion01      = WyrmBaseSource.TargetOcclusion01,
        };
        JobHandle applyHandle = applyJob.Schedule(sourceActiveCount, 32, resolveHandle);

        return applyHandle;
    }
}
