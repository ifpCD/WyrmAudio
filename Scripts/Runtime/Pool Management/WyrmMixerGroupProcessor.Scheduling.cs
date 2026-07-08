using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    internal void UpdateTrackedTransform(int index, Transform track)
    {
        // safety bounds check
        if (index < 0 || index >= TrackedTransforms.length) return;

        // fallbkac to the processor's transform.
        TrackedTransforms[index] = track != null ? track : _cachedTransform;
    }

    bool _hasFocus = true;

    void OnApplicationFocus(bool hasFocus) => _hasFocus = hasFocus;

    internal void CullSources()
    {
        if (!_hasFocus) return;

        double currentTime = AudioSettings.dspTime;
        for (int index = _activeCount - 1; index >= 0; index--)
        {
            if (currentTime < PlaybackEndTimes[index]) continue;

            var source = _activeSources[index];

            if (source.IsBorrowed || source.isPlaying)
                continue;

            ReturnActiveSourceAtIndex(index);
        }
    }

    internal void LateUpdateJobs()
    {
        if (_activeCount == 0) return;

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
        JobHandle applyHandle = applyTransformsJob.Schedule(SourceTransforms, dependsOn: gatherHandle);

        applyHandle.Complete();
    }
}