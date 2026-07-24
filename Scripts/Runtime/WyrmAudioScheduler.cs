using System;
using SteamAudio;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(3)]
public sealed class WyrmAudioScheduler : MonoBehaviour
{
    bool _hasFocus = true;

    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

    void LateUpdate()
    {
        if (WyrmBaseSource.EnabledInstanceCount == 0)
            return;

        CullSources();

        if (WyrmBaseSource.EnabledInstanceCount == 0)
            return;

        WyrmListener.Synchronize();
        ScheduleFrame().Complete();
        SubmitNativeAudio();
    }

    void CullSources()
    {
        if (!_hasFocus)
            return;

        double currentTime = UnityEngine.AudioSettings.dspTime;
        for (int index = WyrmBaseSource.EnabledInstanceCount - 1; index >= 0; index--)
        {
            if (currentTime < WyrmBaseSource.PlaybackEndTimes[index])
                continue;

            WyrmBaseSource source = WyrmBaseSource.ActiveSources[index];
            if (source.IsBorrowed || source.isPlaying)
                continue;

            source.Return();
        }
    }

    static JobHandle ScheduleFrame()
    {
        var gatherTrackedPositions = new GatherTrackedPositionsJob
        {
            IsTracking = WyrmBaseSource.IsTracking,
            TrackedPositions = WyrmBaseSource.TrackedPositions,
        };
        JobHandle gatherHandle = gatherTrackedPositions.Schedule(WyrmBaseSource.TrackedTransforms);

        var applySourceTransforms = new ApplySourceTransformsJob
        {
            IsTracking = WyrmBaseSource.IsTracking,
            TrackedPositions = WyrmBaseSource.TrackedPositions,
            SourcePositions = WyrmBaseSource.SourcePositions,
        };
        JobHandle transformsHandle = applySourceTransforms.Schedule(WyrmBaseSource.SourceTransforms, gatherHandle);

        JobHandle locationHandle = LocationProcessor.Schedule(transformsHandle);

        JobHandle occlusionHandle = OcclusionProcessor.Schedule(transformsHandle);

        JobHandle effectsDependency = JobHandle.CombineDependencies(occlusionHandle, locationHandle);

        JobHandle effectsHandle = EffectsMixingProcessor.Schedule(effectsDependency);

        return LerpProcessor.Schedule(effectsHandle);
    }

    static unsafe void SubmitNativeAudio()
    {
        int activeCount = WyrmBaseSource.EnabledInstanceCount;
        SteamAudioSettings steamAudioSettings = SteamAudioSettings.Singleton;

        if (steamAudioSettings != null)
        {
            WyrmPhononCustomAPI.iplSourceSetCustomPathingBatch(
                activeCount,
                (IntPtr*)WyrmBaseSource.Pointers.GetUnsafeReadOnlyPtr(),
                (float*)WyrmBaseSource.CurrentPropagationEQ01.GetUnsafeReadOnlyPtr(),
                (float*)WyrmBaseSource.TargetSHCoefficients.GetUnsafeReadOnlyPtr(),
                steamAudioSettings.realTimeAmbisonicOrder
            );

            WyrmPhononCustomAPI.iplSourceSetCustomDirectBatch(
                activeCount,
                (IntPtr*)WyrmBaseSource.Pointers.GetUnsafeReadOnlyPtr(),
                (float3*)WyrmBaseSource.SourcePositions.GetUnsafeReadOnlyPtr(),
                null,
                null
            );
        }

        for (int index = 0; index < activeCount; index++)
        {
            if (WyrmBaseSource.ActiveSources[index] is WyrmPhononSource phononSource)
                phononSource.SetOcclusionLevel(WyrmBaseSource.CurrentOcclusion01[index]);
        }
    }
}
