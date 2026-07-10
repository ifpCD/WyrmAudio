using System;
using SteamAudio;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmPoolController : MonoBehaviour
{
    void LateUpdate()
    {
        if (IsDisposed) return;
        if (ActiveCount == 0) return;
        CullSources();
        ScheduleJobs();
    }

    private void CullSources()
    {
        if (!_hasFocus) return;

        double currentTime = UnityEngine.AudioSettings.dspTime;
        for (int index = ActiveCount - 1; index >= 0; index--)
        {
            if (currentTime < PlaybackEndTimes[index]) continue;

            var source = ActiveSources[index];
            if (source.IsBorrowed || source.isPlaying) continue;

            source.Return();
        }
    }

    private void ScheduleJobs()
    {
        var gatherJob = new GatherTrackedPositionsJob
        {
            IsTracking = IsTracking,
            TrackedPositions = TrackedPositions
        };
        JobHandle gatherHandle = gatherJob.Schedule(TrackedTransforms);

        var applyTransformsJob = new ApplySourceTransformsJob
        {
            IsTracking = IsTracking,
            TrackedPositions = TrackedPositions,
            SourcePositions = SourcePositions
        };
        JobHandle applyTransformsHandle = applyTransformsJob.Schedule(SourceTransforms, gatherHandle);

        applyTransformsHandle.Complete();

        JobHandle roomJobs = WyrmRoomManager.Instance.SchedulePropagation();
        roomJobs.Complete();

        unsafe
        {
            WyrmPhononCustomAPI.iplSourceSetCustomPathingBatch(
                ActiveCount,
                (IntPtr*)SourceHandles.GetUnsafeReadOnlyPtr(),
                (float*)PropagationPathEQs.GetUnsafeReadOnlyPtr(),
                (float*)PropagationSHCoeffs.GetUnsafeReadOnlyPtr(),
                WyrmAudioSettings.Instance.pathingAmbisonicsOrder
            );
        }

        for (int index = 0; index < ActiveCount; index++)
        {
            var source = ActiveSources[index];
            if (source is WyrmPhononSource phononSource)
            {
                // fine for now
                float3 pos = SourcePositions[index];
                float3 fwd = source.CachedTransform.forward;
                float3 up = source.CachedTransform.up;
                float3 right = source.CachedTransform.right;

                phononSource.UpdatePhononSimulatorPosition(pos, fwd, up, right);

                phononSource.SetOcclusionLevel(SourceOcclusions[index]);
            }
        }
    }
}