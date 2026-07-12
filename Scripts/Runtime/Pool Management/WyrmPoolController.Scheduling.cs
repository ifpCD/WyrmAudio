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
                SteamAudioSettings.Singleton.realTimeAmbisonicOrder
            );
        }

        for (int index = 0; index < ActiveCount; index++)
        {
            var source = ActiveSources[index];
            if (source is WyrmPhononSource phononSource)
            {
                phononSource.UpdatePhononSimulator(SourcePositions[index]);
                phononSource.SetOcclusionLevel(SourceOcclusions[index]);
            }
        }
    }
}