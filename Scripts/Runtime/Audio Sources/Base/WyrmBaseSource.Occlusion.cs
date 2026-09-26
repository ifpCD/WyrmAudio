using UnityEngine;

public partial class WyrmBaseSource
{
    [SerializeField]
    [Range(0f, 1f)]
    protected float _occlusionValue = 0f;

    public virtual float OcclusionValue
    {
        get => _occlusionValue;
        set
        {
            if (_occlusionValue == value)
                return;

            _occlusionValue = value;
        }
    }

    [SerializeField]
    WyrmOcclusionMask _occlusionMask;

    internal WyrmOcclusionMask OcclusionMask
    {
        get => _occlusionMask;
        set
        {
            if (_occlusionMask == value)
                return;
            _occlusionMask = value;

            if (IsRegistered)
                OcclusionMaskHandles[SoAIndex] = OcclusionMaskHandle;
        }
    }

    internal AmbiHandle OcclusionMaskHandle => (UseOcclusion && _occlusionMask != null) ? OcclusionMask.Handle : AmbiHandle.Null;
}
