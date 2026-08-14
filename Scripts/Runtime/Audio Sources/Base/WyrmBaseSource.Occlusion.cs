using UnityEngine;

public partial class WyrmBaseSource
{
    [SerializeField]
    WyrmOcclusionMask _mask;

    internal WyrmOcclusionMask Mask
    {
        get => _mask;
        set
        {
            if (_mask == value)
                return;

            _mask = value;

            if (IsRegistered)
                OcclusionMaskIndex[SoAIndex] = (value != null && value.IsRegistered) ? value.SoAIndex : -1;
        }
    }
}
