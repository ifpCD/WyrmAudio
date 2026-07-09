using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Jobs;


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
            IsTracking[index] = 0;
            source.CachedTransform.position = staticPosition;
            TrackedPositions[index] = staticPosition;
            SourcePositions[index] = staticPosition;
        }
        else
        {
            IsTracking[index] = 1;
            SourcePositions[index] = source.CachedPosition;
            TrackedPositions[index] = trackTransform != null ? trackTransform.position : CachedTransform.position;
        }

        SourceActiveStates[index] = 1;
        SourceMinDistances[index] = source.minDistance;
        SourceMaxDistances[index] = source.maxDistance;
        OutputNormalizedRoomMixVolume[index] = 0f;
        PlaybackEndTimes[index] = double.MaxValue;

        ActiveCount++;
    }

    internal void ReturnSource(IWyrmSource source)
    {
        if (IsDisposed) return;

        int index = source.ActiveIndex;
        if (index < 0 || index >= ActiveCount || ActiveSources[index] != source) return;

        int lastIndex = ActiveCount - 1;

        if (index != lastIndex) // Swap Data
        {
            var swappedSource = ActiveSources[lastIndex];
            swappedSource.ActiveIndex = index;
            ActiveSources[index] = swappedSource;

            SourcePositions[index] = SourcePositions[lastIndex];
            TrackedPositions[index] = TrackedPositions[lastIndex];
            IsTracking[index] = IsTracking[lastIndex];
            PropagationPositions[index] = PropagationPositions[lastIndex];
            SourceActiveStates[index] = SourceActiveStates[lastIndex];
            SourceMinDistances[index] = SourceMinDistances[lastIndex];
            SourceMaxDistances[index] = SourceMaxDistances[lastIndex];
            OutputNormalizedRoomMixVolume[index] = OutputNormalizedRoomMixVolume[lastIndex];
            PlaybackEndTimes[index] = PlaybackEndTimes[lastIndex];
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