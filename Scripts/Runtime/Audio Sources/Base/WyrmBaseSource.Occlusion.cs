using UnityEngine;

public partial class WyrmBaseSource
{
    [SerializeField]
    WyrmOcclusionMask _mask;

    WyrmOcclusionMask Mask
    {
        get => _mask;
        set
        {
            if (_mask == value)
                return;

            _mask = value;

            if (value == null)
                return;

            // if (IsRegistered)
        }
    }

    void EnsureMask()
    {
        if (!UseOcclusion)
            return;

        if (Mask != null)
            return;
    }
}
