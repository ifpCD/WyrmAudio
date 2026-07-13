using System;
using Unity.Mathematics;
using UnityEngine;

// Native Arrays break down into two kinds
// 1. Inputs which are declared during activation that the source might attempt to modify
// 2. Outputs which are fresh and are always generated after any of these operations happen (100 execution order in LateUpdate)
// Outputs can be stale because they get overwritten anyways.
public partial class WyrmPoolController : MonoBehaviour
{
    internal void ActivateSource(IWyrmSource source, bool isTracking, Vector3 staticPosition, Transform trackTransform)
    {
        int index = ActiveCount;

        source.ActiveIndex = index;
        ActiveSources[index] = source;

        SourceTransforms.Add(source.CachedTransform);
        TrackedTransforms.Add(trackTransform != null ? trackTransform : CachedTransform);

        if (!isTracking)
        {
            IsSourceTrackingTransform[index] = 0;
            source.CachedTransform.position = staticPosition;
            TrackedPositions[index] = staticPosition;
            SourcePositions[index] = staticPosition;
        }
        else
        {
            IsSourceTrackingTransform[index] = 1;
            SourcePositions[index] = source.CachedPosition;
            TrackedPositions[index] = trackTransform != null ? trackTransform.position : CachedTransform.position;
        }

        MinDistances[index] = source.minDistance;
        MaxDistances[index] = source.maxDistance;

        PlaybackEndTimes[index] = double.MaxValue;

        UseOcclusions[index] = Convert.ToByte(source.UseOcclusion);
        UsePropagations[index] = Convert.ToByte(source.UsePropagation);

        if (source is WyrmPhononSource phononSource && phononSource.PhononSource != null)
            Pointers[index] = phononSource.PhononSource.Get();
        else
            Pointers[index] = IntPtr.Zero;

        ActiveCount++;
    }

    internal void ReturnSource(IWyrmSource source)
    {
        if (IsDisposed) return;

        int index = source.ActiveIndex;
        if (index < 0 || index >= ActiveCount || ActiveSources[index] != source) return;

        int lastIndex = ActiveCount - 1;

        if (index != lastIndex)
        {
            var swappedSource = ActiveSources[lastIndex];
            swappedSource.ActiveIndex = index;

            // Managed Arrays
            ActiveSources[index] = swappedSource;
            PlaybackEndTimes[index] = PlaybackEndTimes[lastIndex];

            // Source Inputs
            MinDistances[index] = MinDistances[lastIndex];
            MaxDistances[index] = MaxDistances[lastIndex];

            UsePropagations[index] = UsePropagations[lastIndex];
            UseOcclusions[index] = UseOcclusions[lastIndex];

            IsSourceTrackingTransform[index] = IsSourceTrackingTransform[lastIndex];

            SourcePositions[index] = SourcePositions[lastIndex];
            TrackedPositions[index] = TrackedPositions[lastIndex];

            // OcclusionCommands[index] = OcclusionCommands[lastIndex];
            // OcclusionHitResults[index] = OcclusionHitResults[lastIndex];

            // SourceRoomIdentifiers[index] = SourceRoomIdentifiers[lastIndex];
            // PropagationDirections[index] = PropagationDirections[lastIndex];
            // PropagationDistances[index] = PropagationDistances[lastIndex];

            // TargetOcclusion01[index] = TargetOcclusion01[lastIndex];

            Pointers[index] = Pointers[lastIndex];
            // TargetPropagationEQ01[index] = TargetPropagationEQ01[lastIndex];

            CurrentOcclusion01[index] = CurrentOcclusion01[lastIndex];
            CurrentPropagationEQ01s[index] = CurrentPropagationEQ01s[lastIndex];


            int targetStride = index * 16;
            int lastStride = lastIndex * 16;
            for (int i = 0; i < 16; i++)
            {
                PropagationSHCoeffOutputs[targetStride + i] = PropagationSHCoeffOutputs[lastStride + i];
            }
        }

        ActiveSources[lastIndex] = null;
        SourceTransforms.RemoveAtSwapBack(index);
        TrackedTransforms.RemoveAtSwapBack(index);

        ActiveCount--;

        source.Deactivate();
        source.ActiveIndex = -1;
        source.IsBorrowed = false;
    }
}