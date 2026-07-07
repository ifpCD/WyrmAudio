using System.Collections.Generic;
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

        for (int index = _activeCount - 1; index >= 0; index--)
        {
            if (!_activeSources[index].IsBorrowed && !_activeSources[index].isPlaying)
            {
                ReturnActiveSourceAtIndex(index);
            }
        }
    }

    internal void LateUpdateJobs()
    {
        if (_activeCount == 0) return;
        
        var gatherJob = new GatherTrackedPositionsJob
        {
            TrackedPositions = TrackedPositions
        };
        JobHandle gatherHandle = gatherJob.Schedule(TrackedTransforms);

        var applyTransformsJob = new ApplySourceTransformsJob
        {
            TrackedPositions = TrackedPositions,
            SourcePositions = SourcePositions
        };
        JobHandle applyHandle = applyTransformsJob.Schedule(SourceTransforms, dependsOn: gatherHandle);

        applyHandle.Complete();
    }
}