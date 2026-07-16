using UnityEngine;

public partial class WyrmBaseSource : MonoBehaviour, IWyrmSource
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

            if (ActiveIndex >= 0)
                WyrmPoolController.UpdateTrackedTransform(ActiveIndex, _trackedTransform);
        }
    }

    public Vector3 CachedPosition { get; set; }
    public Quaternion CachedRotation { get; set; }
}