using System;
using SteamAudio;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[DefaultExecutionOrder(10)]
public sealed partial class WyrmAudioScheduler : MonoBehaviour
{
    bool _hasFocus = true;

    JobHandle handle = default;

    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

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
            WyrmBaseSource source = WyrmBaseSource.RegisteredInstances[index];

            if (source.IsBorrowed || currentTime < source.PlaybackEndTime)
                continue;

            // interop cost, so we do this last after checking C# values
            if (source.isPlaying)
                continue;

            if (source.IsPooled)
                source.Return();
            else
                source.NotifyPlaybackCompletion();
        }
    }

    // csharpier-ignore
    static JobHandle ScheduleFrame()
    {
        var gatherSourcePositions   = new GatherSourcePositionsJob
        {
            Positions               = WyrmBaseSource.Positions,
            Rotations               = WyrmBaseSource.Rotations,
            LocalToWorlds           = WyrmBaseSource.LocalToWorlds
        };
        JobHandle gatherHandle      = gatherSourcePositions.Schedule(WyrmBaseSource.PositionTransforms);

        // we use WyrmBaseSource.SourcePositions for every job - meaning we can append this to the finalizer handle
        var applySourceTransforms   = new ApplySourceTransformsJob
        {
            Positions               = WyrmBaseSource.Positions,
            Rotations               = WyrmBaseSource.Rotations,
        };
        JobHandle transformsHandle  = applySourceTransforms.Schedule(WyrmBaseSource.SourceTransforms, gatherHandle);

        JobHandle locationHandle    = LocationProcessor.Schedule(gatherHandle);

        JobHandle occlusionHandle   = OcclusionProcessor.Schedule(gatherHandle);

        JobHandle ambisonicHandle   = AmbisonicProcessor.Schedule(locationHandle);

        JobHandle effectsDependency = JobHandle.CombineDependencies(locationHandle, occlusionHandle, ambisonicHandle);

        // JobHandle effectsHandle     = EffectsMixingProcessor.Schedule(effectsDependency);

        JobHandle lerpHandle        = LerpProcessor.Schedule(effectsDependency);

        JobHandle finalizerHandle   = JobHandle.CombineDependencies(lerpHandle, transformsHandle);

        return finalizerHandle;
    }

    static unsafe void SubmitNativeAudio()
    {
        int activeCount = WyrmBaseSource.ActiveCount;

        // audioplugin_phonon constantly overrides occlusion/transmission values even if we set them through the custom API
        // so for now we just do this
        for (int index = 0; index < activeCount; index++)
        {
            if (WyrmBaseSource.RegisteredInstances[index] is WyrmPhononSource phononSource)
                phononSource.OcclusionValue = WyrmBaseSource.CurrentOcclusion01[index];
        }

        SteamAudioSettings steamAudioSettings = SteamAudioSettings.Singleton;
        if (steamAudioSettings == null)
            return;

        WyrmPhononCustomAPI.iplSourceSetCustomPathingBatch(
            activeCount,
            (IntPtr*)WyrmBaseSource.SpatializerPointers.GetUnsafeReadOnlyPtr(),
            (float*)WyrmBaseSource.CurrentAmbisonicEQ01s.GetUnsafeReadOnlyPtr(),
            (float*)WyrmBaseSource.TargetAmbisonic.GetUnsafeReadOnlyPtr(),
            steamAudioSettings.realTimeAmbisonicOrder
        );

        WyrmPhononCustomAPI.iplSourceSetCustomDirectBatch(
            activeCount,
            (IntPtr*)WyrmBaseSource.SpatializerPointers.GetUnsafeReadOnlyPtr(),
            (float3*)WyrmBaseSource.Positions.GetUnsafeReadOnlyPtr(),
            null,
            null
        );
    }
}
