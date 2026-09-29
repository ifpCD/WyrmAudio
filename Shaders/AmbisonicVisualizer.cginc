#ifndef WYRM_AMBISONIC_VISUALIZER_INCLUDED
#define WYRM_AMBISONIC_VISUALIZER_INCLUDED

#include "UnityCG.cginc"

// must match HC.MAX_AMBISONIC_CHANNELS
#define MAX_CHANNELS 121

// xyz: low, mid, high band coefficients in ACN order
float4 _Field[MAX_CHANNELS];
// index l(l+1)+m: xy = degree recurrence (a, b) for l > m, z = sectoral factor for l == m
float4 _Recurrence[MAX_CHANNELS];
float _Order;

float _BaseRadius;
float _DeformScale;
float _RadiusMode;
float _Sensitivity;
float _RangeDecibels;
float _FieldPeak;

// Same basis as SphericalHarmonics.Evaluate (orthonormal, ACN, no Condon-Shortley phase), constants from the CPU.
float4 EvaluateField(float3 direction)
{
    // Unity (+x right, +y up, +z forward) -> ambisonic (+x forward, +y left, +z up)
    float x = direction.z;
    float y = -direction.x;
    float z = direction.y;

    int order = (int)_Order;
    float4 sum = 0;
    float sectoral = 0.2820947918;
    float cosine = 1;
    float sine = 0;

    [loop]
    for (int m = 0; m <= order; m++)
    {
        if (m > 0)
        {
            float rotatedCosine = cosine * x - sine * y;
            sine = cosine * y + sine * x;
            cosine = rotatedCosine;
            sectoral *= _Recurrence[m * (m + 2)].z;
        }

        float previous = 0;
        float current = sectoral;

        [loop]
        for (int l = m; l <= order; l++)
        {
            int center = l * (l + 1);

            if (l > m)
            {
                float2 recurrence = _Recurrence[center + m].xy;
                float next = recurrence.x * z * current - recurrence.y * previous;
                previous = current;
                current = next;
            }

            if (m == 0)
                sum += current * _Field[center];
            else
                sum += current * (cosine * _Field[center + m] + sine * _Field[center - m]);
        }
    }

    return sum;
}

float Decibels(float magnitude)
{
    return 20.0 * log10(max(magnitude, 1e-9) / max(_FieldPeak, 1e-9));
}

// 0 on the base sphere, 1 at the peak lobe; a silent field rests on the base sphere
float FieldLevel(float4 field)
{
    float magnitude = length(field.xyz);

    if (_RadiusMode < 0.5)
        return saturate(pow(magnitude * _Sensitivity, 0.8));

    return _FieldPeak > 1e-9 ? saturate(1.0 + Decibels(magnitude) / _RangeDecibels) : 0.0;
}

float Presence(float level)
{
    return saturate(level * 2.0);
}

float3 SurfacePosition(float3 direction, float level)
{
    return direction * (_BaseRadius + level * _DeformScale);
}

// Squeezes the visualizer into the nearest depth slice: it overlays the scene, yet still occludes itself.
float4 OverlayClipPosition(float3 objectPosition)
{
    float4 clip = UnityObjectToClipPos(float4(objectPosition, 1.0));
    clip.z = clip.w * (UNITY_NEAR_CLIP_VALUE + (clip.z / clip.w - UNITY_NEAR_CLIP_VALUE) * 0.001);
    return clip;
}

#endif
