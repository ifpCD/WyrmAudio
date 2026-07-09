using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;

public partial class WyrmPoolController : MonoBehaviour
{
    void LateUpdate()
    {
        if (IsDisposed) return;
        CullSources();
        LateUpdateJobs();
    }

    private void CullSources()
    {
        if (!_hasFocus || ActiveCount == 0) return;

        double currentTime = AudioSettings.dspTime;
        for (int index = ActiveCount - 1; index >= 0; index--)
        {
            if (currentTime < PlaybackEndTimes[index]) continue;

            var source = ActiveSources[index];
            if (source.IsBorrowed || source.isPlaying) continue;

            source.Return();
        }
    }

    private void LateUpdateJobs()
    {
        if (ActiveCount == 0) return;

        var gatherJob = new GatherTrackedPositionsJob { IsTracking = IsTracking, TrackedPositions = TrackedPositions };
        JobHandle gatherHandle = gatherJob.Schedule(TrackedTransforms);

        var applyTransformsJob = new ApplySourceTransformsJob { IsTracking = IsTracking, TrackedPositions = TrackedPositions, SourcePositions = SourcePositions };
        JobHandle applyHandle = applyTransformsJob.Schedule(SourceTransforms, gatherHandle);

        applyHandle.Complete();

        for (int i = 0; i < ActiveCount; i++)
        {
            var source = ActiveSources[i];

            if (source is WyrmPhononSource phononSource)
            {
                float3 pos = SourcePositions[i];

                float3 fwd = source.CachedTransform.forward;
                float3 up = source.CachedTransform.up;
                float3 right = source.CachedTransform.right;

                phononSource.UpdatePhononSimulatorPosition(pos, fwd, up, right);

                // phononSource.SetOcclusionAndTransmission()
            }
        }
    }
}