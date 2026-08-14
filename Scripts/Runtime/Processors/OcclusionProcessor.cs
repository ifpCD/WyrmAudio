using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive || WyrmListener.CompletelyInactive || WyrmOcclusionMask.CompletelyInactive)
            return dependency;

        int sampleActiveCount = WyrmOcclusionSample.ActiveCount;

        WyrmOcclusionSample.IsDiscarded.SubFill(false, sampleActiveCount);

        var CommandsSubBuffer = WyrmBaseSource.RaycastCommandsBuffer.GetSubArray(0, sampleActiveCount);
        var ResultsSubBuffer = WyrmBaseSource.HitResultsBuffer.GetSubArray(0, sampleActiveCount);

        var layerMask = WyrmAudioSettings.Instance.OcclusionMask;
        var queryParameters = new QueryParameters(layerMask, false, QueryTriggerInteraction.Ignore);

        // csharpier-ignore
        var genDiscardCommandsJob             = new GenerateDiscardCommands
        {
            SampleToMask                      = WyrmOcclusionSample.SampleToMask,
            SampleLocalPositions              = WyrmOcclusionSample.LocalPositions,
            MaskLocalToWorlds                 = WyrmOcclusionMask.LocalToWorlds,
            QueryParameters                   = queryParameters,

            SampleWorldPositions              = WyrmOcclusionSample.WorldPositions,
            SampleDiscardCommands             = CommandsSubBuffer,
        };
        JobHandle genDiscardCommandsHandle = genDiscardCommandsJob.Schedule(sampleActiveCount, 16, dependency);

        JobHandle discardResultsHandle = RaycastCommand.ScheduleBatch(CommandsSubBuffer, ResultsSubBuffer, 16, genDiscardCommandsHandle);

        // csharpier-ignore
        var genOcclusionCommandsJob           = new GenerateOcclusionCommands
        {
            SampleWorldPositions              = WyrmOcclusionSample.WorldPositions,
            ListenerPosition                  = WyrmListener.ListenerPosition,
            QueryParameters                   = queryParameters,
            SampleDiscardResults              = ResultsSubBuffer,

            SampleOcclusionCommands           = CommandsSubBuffer,
            SampleIsDiscarded                 = WyrmOcclusionSample.IsDiscarded,
        };
        JobHandle genOcclusionCommandsHandle = genOcclusionCommandsJob.Schedule(sampleActiveCount, 16, discardResultsHandle);

        JobHandle occlusionResultHandle = RaycastCommand.ScheduleBatch(CommandsSubBuffer, ResultsSubBuffer, 16, genOcclusionCommandsHandle);

        // depend on occlusionResultHandle
        // calculate each masks TargetOcclusionValue01s via
        // figuring out how much each sample weighs in the outcome (sample/non-discarded sample count * sample's weight)
        // then accumulate value of each non-discarded sample, and make sure that it's in 0f to 1f range.

        return occlusionResultHandle;
    }
}
