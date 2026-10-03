using UnityEngine;

public sealed partial class WyrmAmbisonicGenerator
{
    [SerializeField]
    bool _distanceAttenuation = true;

    [SerializeField]
    [Min(0f)]
    float _minDistance = DirectPropagation.DEFAULT_MIN_DISTANCE;

    [SerializeField]
    bool _airAbsorption = true;

    [SerializeField]
    Vector3 _airAbsorptionCoefficients = DirectPropagation.DefaultAirAbsorption;

    public bool DistanceAttenuation
    {
        get => _distanceAttenuation;
        set
        {
            _distanceAttenuation = value;

            if (IsRegistered)
                Propagations[SoAIndex] = Propagation;
        }
    }

    public float MinDistance
    {
        get => _minDistance;
        set
        {
            _minDistance = value;

            if (IsRegistered)
                Propagations[SoAIndex] = Propagation;
        }
    }

    public bool AirAbsorption
    {
        get => _airAbsorption;
        set
        {
            _airAbsorption = value;

            if (IsRegistered)
                Propagations[SoAIndex] = Propagation;
        }
    }

    // per band (x low, y mid, z high): exp(-coefficient * distance)
    public Vector3 AirAbsorptionCoefficients
    {
        get => _airAbsorptionCoefficients;
        set
        {
            _airAbsorptionCoefficients = value;

            if (IsRegistered)
                Propagations[SoAIndex] = Propagation;
        }
    }

    // csharpier-ignore
    DirectPropagation Propagation => new()
    {
        DistanceAttenuation       = _distanceAttenuation,
        AirAbsorption             = _airAbsorption,
        MinDistance               = _minDistance,
        AirAbsorptionCoefficients = _airAbsorptionCoefficients,
    };
}
