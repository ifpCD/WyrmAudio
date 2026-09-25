using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
public partial class WyrmOcclusionSample : AmbiBase<WyrmOcclusionSample>
{
    Vector3 _localPosition;

    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            if (_localPosition == value)
                return;

            _localPosition = value;

            if (IsRegistered)
                InputLocalPositions[SoAIndex] = value;
        }
    }

    public bool Discardable = true;

    [Range(0.25f, 2f)]
    public float Weight = 1f;

    [Range(0, 3)]
    public byte LODGroup = 0;

    ~WyrmOcclusionSample() => Deregister();
}
