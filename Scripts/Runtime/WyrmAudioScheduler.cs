using System;
using SteamAudio;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(1)]
public sealed class WyrmAudioScheduler : MonoBehaviour
{
    bool _hasFocus = true;

    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

    JobHandle handle = default;

    void Update()
    {
        if (WyrmBaseSource.CompletelyInactive)
            return;

        CullSources();

        if (WyrmBaseSource.CompletelyInactive)
            return;

        WyrmListener.Synchronize();
        handle = ScheduleFrame();
    }

    void LateUpdate()
    {
        if (WyrmBaseSource.CompletelyInactive)
            return;

        if (handle == default)
            return;

        handle.Complete();
        SubmitNativeAudio();
        handle = default;
    }
    
    void CullSources()
    {
        if (!_hasFocus)
            return;

        double currentTime = UnityEngine.AudioSettings.dspTime;
        for (int index = WyrmBaseSource.ActiveCount - 1; index >= 0; index--)
        {
            WyrmBaseSource source = WyrmBaseSource.ActiveSources[index];
            if (source.IsBorrowed || source.isPlaying || currentTime < source.PlaybackEndTime)
                continue;

            if (source.IsPooled)
                source.Return();
            else
                source.CompletePlayback();
        }
    }

    // csharpier-ignore
    static JobHandle ScheduleFrame()
    {
        var gatherSourcePositions   = new GatherSourcePositionsJob { SourcePositions = WyrmBaseSource.SourcePositions };
        JobHandle gatherHandle      = gatherSourcePositions.Schedule(WyrmBaseSource.PositionTransforms);

        // we use WyrmBaseSource.SourcePositions for every calculation, so we can append it to the finalizer handle
        var applySourceTransforms   = new ApplySourceTransformsJob { SourcePositions = WyrmBaseSource.SourcePositions };
        JobHandle transformsHandle  = applySourceTransforms.Schedule(WyrmBaseSource.SourceTransforms, gatherHandle);

        JobHandle locationHandle    = LocationProcessor.Schedule(gatherHandle);

        JobHandle occlusionHandle   = OcclusionProcessor.Schedule(gatherHandle);

        JobHandle effectsDependency = JobHandle.CombineDependencies(occlusionHandle, locationHandle);

        JobHandle effectsHandle     = EffectsMixingProcessor.Schedule(effectsDependency);

        JobHandle lerpHandle        = LerpProcessor.Schedule(effectsHandle);

        JobHandle finalizerHandle   = JobHandle.CombineDependencies(lerpHandle, transformsHandle);

        return finalizerHandle;
    }

    static unsafe void SubmitNativeAudio()
    {
        int activeCount = WyrmBaseSource.ActiveCount;
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

        // refactor
        for (int index = 0; index < activeCount; index++)
        {
            if (WyrmBaseSource.ActiveSources[index] is WyrmPhononSource phononSource)
                phononSource.SetOcclusionLevel(WyrmBaseSource.CurrentOcclusion01[index]);
        }
    }
}
