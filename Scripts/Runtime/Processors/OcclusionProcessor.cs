using Unity.Jobs;
using UnityEngine;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        if (WyrmBaseSource.CompletelyInactive || WyrmListener.CompletelyInactive)
            return dependency;

        int sourceActiveCount = WyrmBaseSource.ActiveCount;
        var raycastCommands = WyrmBaseSource.OcclusionRayCommands.GetSubArray(0, sourceActiveCount);
        var raycastResults = WyrmBaseSource.OcclusionHitResults.GetSubArray(0, sourceActiveCount);

        var queryParameters = new QueryParameters(WyrmAudioSettings.Instance.OcclusionMask, false, QueryTriggerInteraction.Ignore);

        // csharpier-ignore
        var prepareRaycastsJob = new GenerateSourceRaycastCommands
        {
            SourcePositions  = WyrmBaseSource.SourcePositions,
            UseOcclusions    = WyrmBaseSource.UseOcclusions,
            ListenerPosition = WyrmListener.ListenerPosition.Value,
            QueryParameters  = queryParameters,

            RaycastCommands  = raycastCommands,
        };
        JobHandle prepareRaycastsHandle = prepareRaycastsJob.Schedule(sourceActiveCount, 16, dependency);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 16, prepareRaycastsHandle);

        // csharpier-ignore
        var resolveOcclusionJob = new ResolveOcclusionJob
        {
            UseOcclusions    = WyrmBaseSource.UseOcclusions,
            RaycastHits      = raycastResults,

            SourceOcclusions = WyrmBaseSource.TargetOcclusion01,
        };
        return resolveOcclusionJob.Schedule(sourceActiveCount, 16, raycastHandle);
    }
}
