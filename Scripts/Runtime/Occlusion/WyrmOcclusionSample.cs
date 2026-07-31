using UnityEngine;

public partial class OcclusionSample : AmbiBase<OcclusionSample>
{
    public Vector3 Offset = Vector3.zero;
    public float Weight = 1f;
    public bool CheckDiscard = false;
    public int LODGroup = 0;
}
