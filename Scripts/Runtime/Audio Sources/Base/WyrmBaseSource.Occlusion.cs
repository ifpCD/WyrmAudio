using UnityEngine;

public partial class WyrmBaseSource
{
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
                OcclusionMaskIndex[SoAIndex] = (value != null && value.IsRegistered) ? value.SoAIndex : -1;
        }
    }
}
