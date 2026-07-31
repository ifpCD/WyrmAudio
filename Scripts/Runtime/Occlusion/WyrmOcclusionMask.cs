using UnityEngine;

enum OcclusionMaskType : byte
{
    Singular,
    Volumetric,
    Custom,
}

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    OcclusionMaskType _type = OcclusionMaskType.Singular;

    Transform TrackedTransform;

    void OnEnable() => Register();

    void OnDisable() => Deregister();
}
