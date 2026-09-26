using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmOcclusionMask.CompletelyInactive || WyrmAudioManager.Listener == null || WyrmBaseSource.CompletelyInactive)
            return dependency;

        int maskActiveCount = WyrmOcclusionMask.ActiveCount;
        int sourceActiveCount = WyrmBaseSource.ActiveCount;

        int totalActiveSamples = WyrmOcclusionMask.TotalActiveSamples;

        if (totalActiveSamples == 0)
            return dependency;

        var commandsSubBuffer = WyrmOcclusionMask.RaycastCommandBuffer.GetSubArray(0, totalActiveSamples);
        var resultsSubBuffer = WyrmOcclusionMask.RaycastResultBuffer.GetSubArray(0, totalActiveSamples);

        var layerMask = WyrmAudioSettings.Instance.OcclusionMask;
        var queryParameters = new QueryParameters(layerMask, false, QueryTriggerInteraction.Ignore);

        var gatherMasksJob = new GatherMaskTransformsJob { LocalToWorlds = WyrmOcclusionMask.LocalToWorlds };
        JobHandle gatherMasksHandle = gatherMasksJob.Schedule(WyrmOcclusionMask.MaskTransforms, dependency);

        var genDiscardCommandsJob = new GenerateDiscardCommands
        {
            MaskSampleCounts = WyrmOcclusionMask.MaskSampleCounts,
            MaskRaycastOffsets = WyrmOcclusionMask.MaskRaycastOffsets,
            MaskLocalToWorlds = WyrmOcclusionMask.LocalToWorlds,
            
            SampleLocalPositions = WyrmOcclusionMask.SampleLocalPositions,
            SampleIsDiscardable = WyrmOcclusionMask.SampleIsDiscardable,
            QueryParameters = queryParameters,

            SampleWorldPositions = WyrmOcclusionMask.SampleWorldPositions,
            RaycastCommands = commandsSubBuffer,
        };
        // Schedule based on MASK count
        JobHandle genDiscardCommandsHandle = genDiscardCommandsJob.Schedule(maskActiveCount, 16, gatherMasksHandle);

        JobHandle discardResultsHandle = RaycastCommand.ScheduleBatch(commandsSubBuffer, resultsSubBuffer, 16, genDiscardCommandsHandle);

        var genOcclusionCommandsJob = new GenerateOcclusionCommands
        {
            MaskSampleCounts = WyrmOcclusionMask.MaskSampleCounts,
            MaskRaycastOffsets = WyrmOcclusionMask.MaskRaycastOffsets,

            SampleWorldPositions = WyrmOcclusionMask.SampleWorldPositions,
            ListenerPosition = WyrmListener.ListenerPosition,
            QueryParameters = queryParameters,
            SampleDiscardResults = resultsSubBuffer,

            SampleIsDiscarded = WyrmOcclusionMask.SampleIsDiscarded,
            SampleOcclusionCommands = commandsSubBuffer,
        };
        JobHandle genOcclusionCommandsHandle = genOcclusionCommandsJob.Schedule(maskActiveCount, 16, discardResultsHandle);

        JobHandle occlusionResultHandle = RaycastCommand.ScheduleBatch(commandsSubBuffer, resultsSubBuffer, 16, genOcclusionCommandsHandle);

        var resolveJob = new ResolveMaskOcclusionValueJob
        {
            MaskSampleCounts = WyrmOcclusionMask.MaskSampleCounts,
            MaskRaycastOffsets = WyrmOcclusionMask.MaskRaycastOffsets,

            RaycastResults = resultsSubBuffer,
            SampleIsDiscarded = WyrmOcclusionMask.SampleIsDiscarded,
            SampleWeights = WyrmOcclusionMask.SampleWeights,

            MaskTargetOcclusions = WyrmOcclusionMask.TargetOcclusionValue01s,
            SampleIsOccluded = WyrmOcclusionMask.SampleIsOccluded,
        };
        JobHandle resolveHandle = resolveJob.Schedule(maskActiveCount, 16, occlusionResultHandle);

        var applyJob = new ApplyMaskOcclusionToSourceJob
        {
            SourceMaskHandles = WyrmBaseSource.OcclusionMaskHandles,
            UseOcclusions = WyrmBaseSource.UseOcclusions,
            MaskHandleToSoA = WyrmOcclusionMask.HandleToSoA,
            MaskVersions = WyrmOcclusionMask.HandleVersions,
            MaskTargetOcclusions = WyrmOcclusionMask.TargetOcclusionValue01s,

            TargetOcclusion01 = WyrmBaseSource.TargetOcclusion01,
        };
        JobHandle applyHandle = applyJob.Schedule(sourceActiveCount, 32, resolveHandle);

        return applyHandle;
    }
}
