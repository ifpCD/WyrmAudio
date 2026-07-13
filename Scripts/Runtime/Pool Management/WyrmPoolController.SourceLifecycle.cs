using System;
using Unity.Mathematics;
using UnityEngine;

// Native Arrays break down into two kinds
// 1. Inputs that come from the managed space, and can be controlled.
// 2. Stateless Outputs which are always generated and used instantly (no need to overwrite during remove at swap back).
// 3. Stateful Outputs like CurrentOcclusion01 which need to be carried over.
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

    internal bool TryReturnSource(IWyrmSource source)
    {
        if (IsDisposed) return false;

        int index = source.ActiveIndex;
        if (index < 0 || index >= ActiveCount || ActiveSources[index] != source)
            return false;

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

            Pointers[index] = Pointers[lastIndex];

            CurrentOcclusion01[index] = CurrentOcclusion01[lastIndex];
            CurrentPropagationEQ01s[index] = CurrentPropagationEQ01s[lastIndex];
        }

        ActiveSources[lastIndex] = null;
        SourceTransforms.RemoveAtSwapBack(index);
        TrackedTransforms.RemoveAtSwapBack(index);

        ActiveCount--;

        source.Deactivate();
        source.ActiveIndex = -1;
        source.IsBorrowed = false;

        return true;
    }
}