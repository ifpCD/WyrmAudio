using UnityEngine;

public partial class WyrmBaseSource
{
    public Transform CachedTransform { get; private set; } = default;

    private Transform _trackedTransform;
    public Transform TrackedTransform
    {
        get => _trackedTransform;
        set
        {
            if (_trackedTransform == value) return;
            _trackedTransform = value;

            if (!IsRegistered)
                return;

            TrackedTransforms[NativeIndex] = value != null ? value : CachedTransform;
        }
    }
}
