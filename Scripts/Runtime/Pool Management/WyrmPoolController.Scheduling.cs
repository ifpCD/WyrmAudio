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
        JobHandle finalizerHandle = default;

        var gatherJob = new GatherTrackedPositionsJob
        {
            IsTracking = IsSourceTrackingTransform,
            TrackedPositions = TrackedPositions
        };
        JobHandle gatherHandle = gatherJob.Schedule(TrackedTransforms);

        var applyTransformsJob = new ApplySourceTransformsJob
        {
            IsTracking = IsSourceTrackingTransform,
            TrackedPositions = TrackedPositions,
            SourcePositions = SourcePositions
        };
        JobHandle applyTransformsHandle = applyTransformsJob.Schedule(SourceTransforms, gatherHandle);
        applyTransformsHandle.Complete();

        JobHandle effectsHandle = WyrmRoomManager.Instance.ScheduleEffects();
        JobHandle downMixHandle = EffectsMixingProcessor.ScheduleMixing(effectsHandle);
        JobHandle lerpHandle    = LerpProcessor.ScheduleLerping(downMixHandle);

        finalizerHandle = lerpHandle;

        finalizerHandle.Complete();

        unsafe
        {
            WyrmPhononCustomAPI.iplSourceSetCustomPathingBatch(
                ActiveCount,
                (IntPtr*)Pointers.GetUnsafeReadOnlyPtr(),
                (float*)CurrentPropagationEQ01s.GetUnsafeReadOnlyPtr(),
                (float*)PropagationSHCoeffOutputs.GetUnsafeReadOnlyPtr(),
                SteamAudioSettings.Singleton.realTimeAmbisonicOrder
            );

            WyrmPhononCustomAPI.iplSourceSetCustomDirectBatch(
                ActiveCount,
                (IntPtr*)Pointers.GetUnsafeReadOnlyPtr(),
                (float3*)SourcePositions.GetUnsafeReadOnlyPtr(),
                null,
                null
            // (float*)CurrentOcclusion01.GetUnsafeReadOnlyPtr(),
            // (float3*)TargetTransmissionEQ01.GetUnsafeReadOnlyPtr()
            );
        }

        for (int index = 0; index < ActiveCount; index++)
        {
            var source = ActiveSources[index];
            if (source is WyrmPhononSource phononSource)
            {
                phononSource.SetOcclusionLevel(CurrentOcclusion01[index]);
            }
        }
    }
}