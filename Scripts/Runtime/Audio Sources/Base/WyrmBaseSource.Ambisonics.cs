using SaintsField.Playa;
using UnityEngine;

public partial class WyrmBaseSource : AmbiMonoBehaviour<WyrmBaseSource>, IWyrmSource
{
    [SerializeField]
    [Range(0f, 1f)]
    protected float _ambisonicVolume = 0f;

    public virtual float AmbisonicVolume
    {
        get => _ambisonicVolume;
        set
        {
            if (_ambisonicVolume == value)
                return;

            _ambisonicVolume = value;
        }
    }

    [SerializeField]
    WyrmAmbisonicGenerator _ambisonicGenerator;

    internal WyrmAmbisonicGenerator AmbisonicGenerator
    {
        get => _ambisonicGenerator;
        set
        {
            if (_ambisonicGenerator == value)
                return;

            _ambisonicGenerator = value;

            if (IsRegistered)
                AmbisonicGeneratorHandles[SoAIndex] = AmbisonicGeneratorHandle;
        }
    }

    internal AmbiHandle AmbisonicGeneratorHandle => (UseAmbisonics && _ambisonicGenerator != null) ? AmbisonicGenerator.Handle : AmbiHandle.Null;
}
