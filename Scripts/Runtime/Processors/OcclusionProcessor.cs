using Unity.Jobs;
using UnityEngine;

internal static class OcclusionProcessor
{
    public static JobHandle Schedule(JobHandle dependency)
    {
        int activeCount = WyrmBaseSource.EnabledInstanceCount;
        if (activeCount == 0 || WyrmListener.EnabledInstanceCount == 0)
            return dependency;

        var raycastCommands = WyrmBaseSource.OcclusionRayCommands.GetSubArray(0, activeCount);
        var raycastResults = WyrmBaseSource.OcclusionHitResults.GetSubArray(0, activeCount);

        var prepareRaycastsJob = new GenerateSourceRaycastCommands
        {
            SourcePositions = WyrmBaseSource.SourcePositions,
            UseOcclusions = WyrmBaseSource.UseOcclusions,
            ListenerPosition = WyrmListener.ListenerPosition.Value,
            LayerMask = WyrmAudioSettings.Instance.StaticGeometryMask,
            RaycastCommands = raycastCommands,
        };
        JobHandle prepareRaycastsHandle = prepareRaycastsJob.Schedule(activeCount, 16, dependency);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
            raycastCommands,
            raycastResults,
            16,
            prepareRaycastsHandle
        );

        var resolveOcclusionJob = new ResolveOcclusionJob
        {
            UseOcclusions = WyrmBaseSource.UseOcclusions,
            RaycastHits = raycastResults,
            SourceOcclusions = WyrmBaseSource.TargetOcclusion01,
        };
        return resolveOcclusionJob.Schedule(activeCount, 16, raycastHandle);
    }
}
