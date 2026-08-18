using System;
using UnityEngine;

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

            if (IsPooled)
            {
                _trackedTransform = value;
            }
            else
            {
                _trackedTransform = null;
#if UNITY_EDITOR
                Debug.LogWarning("You can't track transforms on non-pooled Wyrm Audio Sources");
#endif
            }

            if (IsRegistered)
                PositionTransforms[SoAIndex] = PositionTransform;
        }
    }

    Transform PositionTransform => _trackedTransform != null ? _trackedTransform : CachedTransform;
}
