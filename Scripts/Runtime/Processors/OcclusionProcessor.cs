using Unity.Jobs;
using UnityEngine;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int activeCount = WyrmBaseSource.ActiveCount;
        if (activeCount == 0 || WyrmListener.ActiveCount == 0)
            return dependency;

        var raycastCommands = WyrmBaseSource.OcclusionRayCommands.GetSubArray(0, activeCount);
        var raycastResults = WyrmBaseSource.OcclusionHitResults.GetSubArray(0, activeCount);

        // csharpier-ignore
        var prepareRaycastsJob = new GenerateSourceRaycastCommands
        {
            SourcePositions  = WyrmBaseSource.SourcePositions,
            UseOcclusions    = WyrmBaseSource.UseOcclusions,
            ListenerPosition = WyrmListener.ListenerPosition.Value,
            LayerMask        = WyrmAudioSettings.Instance.StaticGeometryMask,
            
            RaycastCommands  = raycastCommands,
        };
        JobHandle prepareRaycastsHandle = prepareRaycastsJob.Schedule(activeCount, 16, dependency);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 16, prepareRaycastsHandle);

        // csharpier-ignore
        var resolveOcclusionJob = new ResolveOcclusionJob
        {
            UseOcclusions    = WyrmBaseSource.UseOcclusions,
            RaycastHits      = raycastResults,
            SourceOcclusions = WyrmBaseSource.TargetOcclusion01,
        };
        return resolveOcclusionJob.Schedule(activeCount, 16, raycastHandle);
    }
}
