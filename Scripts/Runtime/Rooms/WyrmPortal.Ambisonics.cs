using SaintsField.Playa;
using UnityEngine;

public partial class WyrmPortal
{
    [SerializeField]
    WyrmAmbisonicGenerator _ambisonicGenerator;

    public WyrmAmbisonicGenerator AmbisonicGenerator
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

    internal AmbiHandle AmbisonicGeneratorHandle => (AmbisonicGenerator != null) ? AmbisonicGenerator.Handle : AmbiHandle.Null;
}
