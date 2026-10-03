using Unity.Mathematics;

// Phonon's direct path without the callbacks: DistanceAttenuationModel and AirAbsorptionModel as DirectSimulator evaluates them,
// split by DirectEffect into a gain and an EQ that EQEffect::normalizeGains floors 24 dB below its loudest band, then folded back
// into the per-band gains the gain and EQ effects would apply.
internal struct DirectPropagation
{
    public const float DEFAULT_MIN_DISTANCE = 1f;

    public static float3 DefaultAirAbsorption => new(0.0002f, 0.0017f, 0.0182f);

    const float MIN_EQ_GAIN = 0.0625f;
    const float FLT_MIN = 1.17549435e-38f;

    public bool DistanceAttenuation;
    public bool AirAbsorption;
    public float MinDistance;
    public float3 AirAbsorptionCoefficients;

    public readonly float3 Evaluate(float distance)
    {
        float gain = DistanceAttenuation ? 1f / math.max(distance, MinDistance) : 1f;

        if (!AirAbsorption)
            return gain;

        float3 eq = math.exp(-AirAbsorptionCoefficients * distance);
        float maxGain = math.cmax(eq);

        if (maxGain < FLT_MIN)
            return 0f;

        return gain * maxGain * math.max(eq / maxGain, MIN_EQ_GAIN);
    }
}
