using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    private struct TransformUpdate
    {
        public IWyrmSource Source;
        public Transform NewTransform;
    }

    internal void QueueTransformUpdate(IWyrmSource source, Transform newTransform)
    {
        var transformUpdate = new TransformUpdate
        {
            Source = source,
            NewTransform = newTransform
        };
        _pendingTransformUpdates.Enqueue(transformUpdate);
    }

    internal void CullSources()
    {

        for (int index = _activeCount - 1; index >= 0; index--)
        {
            if (!_activeSources[index].isPlaying)
            {
                ReturnActiveSourceAtIndex(index);
            }
        }
    }



    internal void UpdateJobs()
    {
        while (_pendingTransformUpdates.Count > 0)
        {
            var request = _pendingTransformUpdates.Dequeue();

            if (request.Source.isPlaying && _activeSources[request.Source.ActiveIndex] == request.Source)
            {
                TrackedTransforms[request.Source.ActiveIndex] = request.NewTransform;
            }
        }

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