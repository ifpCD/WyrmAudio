using System;
using SteamAudio;
using Unity.Collections;
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


        for (int index = 0; index < ActiveCount; index++)
        {
            var source = ActiveSources[index];
            if (source is WyrmPhononSource phononSource)
            {
                ApplyCustomPathingToSources(PropagationDirections, PropagationDistances, PropagationPathEQs);

                float3 pos = SourcePositions[index];
                float3 fwd = source.CachedTransform.forward;
                float3 up = source.CachedTransform.up;
                float3 right = source.CachedTransform.right;

                phononSource.UpdatePhononSimulatorPosition(pos, fwd, up, right);

                phononSource.SetOcclusionLevel(SourceOcclusions[index]);
            }
        }
    }

    private unsafe void ApplyCustomPathingToSources(NativeArray<float3> portalDirections, NativeArray<float> pathDistances, NativeArray<float3> pathEQs)
    {
        float* eqCoeffs = stackalloc float[3];
        float* shCoeffs = stackalloc float[16];

        NativeArray<float> tempSH = new(4, Allocator.Temp);

        for (int i = 0; i < ActiveCount; i++)
        {
            var phononSource = ActiveSources[i] as WyrmPhononSource;
            if (phononSource == null || !phononSource.Pathing)
                continue;

            float dist = pathDistances[i];
            float gain = 1.0f / math.max(dist, 1.0f);

            WyrmCustomPathing.ProjectToAmbisonics(
                portalDirections[i],
                1,
                gain,
                ref tempSH);

            for (int s = 0; s < 4; s++)
                shCoeffs[s] = tempSH[s];

            eqCoeffs[0] = pathEQs[i].x;
            eqCoeffs[1] = pathEQs[i].y;
            eqCoeffs[2] = pathEQs[i].z;

            IntPtr sourceHandle = phononSource.PhononSource.Get();

            WyrmCustomPathing.iplSourceSetCustomPathing(
                sourceHandle,
                eqCoeffs,
                shCoeffs);
        }

        tempSH.Dispose();
    }
}