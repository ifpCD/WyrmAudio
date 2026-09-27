using System.Runtime.CompilerServices;
using Unity.Mathematics;

// Real, orthonormal, ACN, no Condon-Shortley phase: bit-compatible with Phonon's modified Google SH.
internal static unsafe class SphericalHarmonics
{
    public const float Y00 = 0.2820947918f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Evaluate(float3 unitDirection, float* basis)
    {
        // Unity (+x right, +y up, +z forward) -> ambisonic (+x forward, +y left, +z up)
        float x = unitDirection.z;
        float y = -unitDirection.x;
        float z = unitDirection.y;

        float sectoral = Y00;
        float cosine = 1f;
        float sine = 0f;

        for (int m = 0; m <= HC.MAX_AMBISONIC_ORDER; m++)
        {
            if (m > 0)
            {
                float rotatedCosine = cosine * x - sine * y;
                sine = cosine * y + sine * x;
                cosine = rotatedCosine;
                sectoral *= m == 1 ? math.sqrt(3f) : math.sqrt((2f * m + 1f) / (2f * m));
            }

            float previous = 0f;
            float current = sectoral;

            for (int l = m; l <= HC.MAX_AMBISONIC_ORDER; l++)
            {
                if (l > m)
                {
                    float next = AscendDegree(l, m, z, current, previous);
                    previous = current;
                    current = next;
                }

                int center = l * (l + 1);

                if (m == 0)
                {
                    basis[center] = current;
                    continue;
                }

                basis[center + m] = current * cosine;
                basis[center - m] = current * sine;
            }
        }
    }

    // Ambisonic frame, double precision: projection tables only.
    public static void EvaluateAmbisonic(double3 direction, double* basis)
    {
        double sectoral = Y00;
        double cosine = 1.0;
        double sine = 0.0;

        for (int m = 0; m <= HC.MAX_AMBISONIC_ORDER; m++)
        {
            if (m > 0)
            {
                double rotatedCosine = cosine * direction.x - sine * direction.y;
                sine = cosine * direction.y + sine * direction.x;
                cosine = rotatedCosine;
                sectoral *= m == 1 ? math.sqrt(3.0) : math.sqrt((2.0 * m + 1.0) / (2.0 * m));
            }

            double previous = 0.0;
            double current = sectoral;

            for (int l = m; l <= HC.MAX_AMBISONIC_ORDER; l++)
            {
                if (l > m)
                {
                    double next;

                    if (l == m + 1)
                        next = math.sqrt(2.0 * m + 3.0) * direction.z * current;
                    else
                    {
                        double l2 = l * l;
                        double m2 = m * m;
                        double a = math.sqrt((4.0 * l2 - 1.0) / (l2 - m2));
                        double b = math.sqrt(((l - 1.0) * (l - 1.0) - m2) * (2.0 * l + 1.0) / ((2.0 * l - 3.0) * (l2 - m2)));
                        next = a * direction.z * current - b * previous;
                    }

                    previous = current;
                    current = next;
                }

                int center = l * (l + 1);

                if (m == 0)
                {
                    basis[center] = current;
                    continue;
                }

                basis[center + m] = current * cosine;
                basis[center - m] = current * sine;
            }
        }
    }

    // Unity (+x right, +y up, +z forward) -> ambisonic (+x forward, +y left, +z up)
    public static float4x4 AmbisonicFromUnity => new(0f, 0f, 1f, 0f, -1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f);

    // listener -> source vector of any length; coincident positions are omnidirectional
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EvaluateArrival(float3 arrival, float* basis)
    {
        float lengthSquared = math.lengthsq(arrival);

        if (lengthSquared > 1e-12f)
            Evaluate(arrival * math.rsqrt(lengthSquared), basis);
        else
            EvaluateOmnidirectional(basis);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EvaluateOmnidirectional(float* basis)
    {
        basis[0] = Y00;

        for (int channel = 1; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
            basis[channel] = 0f;
    }

    // Exact diagonal filters: spherical heat kernel per degree (isotropic spread), azimuthal Gaussian per |order| (horizontal spread).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shape(float* basis, float gain, float spread, float horizontalSpread, float* output)
    {
        float degreeRate = 0.5f * spread * spread;
        float orderRate = 0.5f * horizontalSpread * horizontalSpread;

        for (int l = 0; l <= HC.MAX_AMBISONIC_ORDER; l++)
        {
            int center = l * (l + 1);
            float degreeGain = gain * math.exp(-degreeRate * (l * (l + 1)));

            output[center] = basis[center] * degreeGain;

            for (int m = 1; m <= l; m++)
            {
                float orderGain = degreeGain * math.exp(-orderRate * (m * m));
                output[center + m] = basis[center + m] * orderGain;
                output[center - m] = basis[center - m] * orderGain;
            }
        }
    }

    // Normalized associated Legendre recurrence (sin^m factored into the sectoral terms).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float AscendDegree(int l, int m, float z, float current, float previous)
    {
        if (l == m + 1)
            return math.sqrt(2f * m + 3f) * z * current;

        float l2 = l * l;
        float m2 = m * m;
        float a = math.sqrt((4f * l2 - 1f) / (l2 - m2));
        float b = math.sqrt(((l - 1f) * (l - 1f) - m2) * (2f * l + 1f) / ((2f * l - 3f) * (l2 - m2)));

        return a * z * current - b * previous;
    }
}
