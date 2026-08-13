using System;
using SaintsField.Playa;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmBaseSource
{
    public Transform CachedTransform { get; private set; }

    [SerializeField]
    Transform _trackedTransform;

    public Transform TrackedTransform
    {
        get => _trackedTransform;
        set
        {
            if (_trackedTransform == value)
                return;

            _trackedTransform = value;

            if (IsRegistered)
                PositionTransforms[SoAIndex] = PositionTransform;
        }
    }

    Transform PositionTransform => _trackedTransform != null ? _trackedTransform : CachedTransform;
}
