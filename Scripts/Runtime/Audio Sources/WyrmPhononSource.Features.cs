using SteamAudio;
using UnityEngine;

public sealed partial class WyrmPhononSource : WyrmBaseSource
{
    public override float OcclusionValue
    {
        set
        {
            if (_occlusionValue == value)
                return;

            _occlusionValue = value;
            SetSpatialValue(OCCLUSION, value);
        }
    }
}
