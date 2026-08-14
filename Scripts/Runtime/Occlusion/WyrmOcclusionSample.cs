using UnityEngine;

public partial class WyrmOcclusionSample : AmbiBase<WyrmOcclusionSample>
{
    public Vector3 LocalPosition = Vector3.zero;

    public bool Discardable = true;

    [Range(0.25f, 2f)]
    public float Weight = 1f;

    public int LODGroup = 0;

    ~WyrmOcclusionSample() => Deregister();
}
