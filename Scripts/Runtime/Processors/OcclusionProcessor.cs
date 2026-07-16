using Unity.Jobs;
using UnityEngine;

internal class OcclusionProcessor
{
    public JobHandle ScheduleSourceOcclusion(JobHandle? appendTo = default)
    {
        var prepareRaycastsJob = new GenerateSourceRaycastCommands
        {
            SourcePositions = WyrmPoolController.Instance.SourcePositions,
            ListenerPosition = WyrmAudioManager.GetAudioListener().transform.position,
            LayerMask = WyrmAudioSettings.Instance.StaticGeometryMask,
            RaycastCommands = WyrmPoolController.Instance.OcclusionRayCommands
        };
        JobHandle prepareRaycastsHandle = prepareRaycastsJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
            WyrmPoolController.Instance.OcclusionRayCommands,
            WyrmPoolController.Instance.OcclusionHitResults,
            16, prepareRaycastsHandle);

        var resolveOcclusionJob = new ResolveOcclusionJob
        {
            RaycastHits = WyrmPoolController.Instance.OcclusionHitResults,
            SourceOcclusions = WyrmPoolController.Instance.TargetOcclusion01
        };
        JobHandle resolveOcclusionHandle = resolveOcclusionJob.Schedule(WyrmPoolController.Instance.ActiveCount, 16, raycastHandle);

        return resolveOcclusionHandle;
    }
}