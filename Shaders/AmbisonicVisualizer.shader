Shader "Hidden/WyrmAudio/AmbisonicVisualizer"
{
    Properties
    {
        _BaseRadius ("Base Bubble Radius", Float) = 2.0
        _DeformScale ("Deformation Scale", Float) = 1.0
        _Sensitivity ("Audio Sensitivity", Float) = 25.0

        _IdleOpacity ("Idle Opacity", Range(0, 0.2)) = 0.02
        _GridIntensity ("Grid Opacity", Range(0, 1)) = 0.3

        [Header(Color Palette)]
        [HDR] _BaseColor ("Idle/Base Color", Color) = (0.02, 0.1, 0.3, 1.0)
        [HDR] _LowColor ("Low Freq (Bass) Color", Color) = (1.0, 0.05, 0.0, 1.0)
        [HDR] _MidColor ("Mid Freq Color", Color) = (0.1, 1.0, 0.3, 1.0)
        [HDR] _HighColor ("High Freq (Treble) Color", Color) = (0.0, 0.8, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            // must match HC.MAX_AMBISONIC_CHANNELS
            #define MAX_CHANNELS 121

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 mappedColor : TEXCOORD0;
                float intensity : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 direction : TEXCOORD4;
                float bandOpacity : TEXCOORD5;
            };

            // xyz: low, mid, high band coefficients in ACN order
            float4 _Field[MAX_CHANNELS];
            float _Order;

            float _BaseRadius;
            float _DeformScale;
            float _Sensitivity;
            float _IdleOpacity;
            float _GridIntensity;

            float4 _BaseColor;
            float4 _LowColor;
            float4 _MidColor;
            float4 _HighColor;

            // Same recurrence as SphericalHarmonics.Evaluate: orthonormal, ACN, no Condon-Shortley phase.
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
                        sectoral *= m == 1 ? 1.7320508 : sqrt((2.0 * m + 1.0) / (2.0 * m));
                    }

                    float previous = 0;
                    float current = sectoral;

                    [loop]
                    for (int l = m; l <= order; l++)
                    {
                        if (l == m + 1)
                        {
                            previous = current;
                            current = sqrt(2.0 * m + 3.0) * z * previous;
                        }
                        else if (l > m + 1)
                        {
                            float l2 = l * l;
                            float m2 = m * m;
                            float a = sqrt((4.0 * l2 - 1.0) / (l2 - m2));
                            float b = sqrt(((l - 1.0) * (l - 1.0) - m2) * (2.0 * l + 1.0) / ((2.0 * l - 3.0) * (l2 - m2)));
                            float next = a * z * current - b * previous;
                            previous = current;
                            current = next;
                        }

                        int center = l * (l + 1);

                        if (m == 0)
                            sum += current * _Field[center];
                        else
                            sum += current * (cosine * _Field[center + m] + sine * _Field[center - m]);
                    }
                }

                return sum;
            }

            v2f vert (appdata v)
            {
                v2f o;

                float3 direction = normalize(v.normal);
                float4 field = EvaluateField(direction) * _Sensitivity;

                if (any(isnan(field)) || any(isinf(field)))
                    field = 0;

                float3 bands = abs(field.xyz);
                float totalBandEnergy = bands.x + bands.y + bands.z + 0.0001;

                // only positive band contributions carry color: negative lobes render black
                float3 positive = max(field.xyz, 0.0);
                o.mappedColor = (positive.x * _LowColor.rgb + positive.y * _MidColor.rgb + positive.z * _HighColor.rgb) / totalBandEnergy;
                o.bandOpacity = (bands.x * _LowColor.a + bands.y * _MidColor.a + bands.z * _HighColor.a) / totalBandEnergy;
                o.intensity = lerp(dot(bands, 1.0 / 3.0), max(bands.x, max(bands.y, bands.z)), 0.5);

                float4 position = float4(direction * (_BaseRadius + pow(o.intensity, 0.8) * _DeformScale), 1.0);

                o.pos = UnityObjectToClipPos(position);
                o.viewDir = normalize(WorldSpaceViewDir(position));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.direction = direction;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 direction = normalize(i.direction);
                float2 sphereUV = float2(atan2(direction.z, direction.x) * (0.5 / UNITY_PI) + 0.5, acos(clamp(direction.y, -1.0, 1.0)) / UNITY_PI);

                float audioGlow = saturate(i.intensity);
                float3 baseColor = lerp(_BaseColor.rgb, i.mappedColor, audioGlow);

                float2 gridUV = frac(sphereUV * 24.0);
                float horizontalLines = smoothstep(0.95, 1.0, gridUV.y);
                float verticalLines = smoothstep(0.95, 1.0, gridUV.x);

                float poleFade = smoothstep(0.02, 0.15, sphereUV.y) * smoothstep(0.98, 0.85, sphereUV.y);
                verticalLines *= poleFade;

                float gridLine = saturate(horizontalLines + verticalLines);
                gridLine *= _GridIntensity * (0.2 + audioGlow * 1.5);

                float pulse = sin(direction.y * 20.0 - _Time.y * 15.0) * 0.5 + 0.5;
                float pulseGlow = pulse * audioGlow * 0.5;

                float NdotV = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
                float rimGlow = smoothstep(0.5, 1.0, 1.0 - NdotV) * (0.1 + audioGlow * 1.5);

                float3 finalColor = baseColor + (baseColor * gridLine) + (i.mappedColor * pulseGlow) + (i.mappedColor * rimGlow);

                float effectiveOpacity = max(_IdleOpacity, i.bandOpacity * audioGlow);

                float alpha = _IdleOpacity + (audioGlow * i.bandOpacity);
                alpha += gridLine * effectiveOpacity;
                alpha += rimGlow * effectiveOpacity;

                return fixed4(finalColor, saturate(alpha));
            }
            ENDCG
        }
    }
}
