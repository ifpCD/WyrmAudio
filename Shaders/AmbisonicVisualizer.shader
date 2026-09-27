Shader "Hidden/WyrmAudio/AmbisonicVisualizer"
{
    Properties
    {
        _BaseRadius ("Base Radius", Float) = 2.0
        _DeformScale ("Deformation Scale", Float) = 1.5
        _RadiusMode ("Radius Mode (0 linear, 1 decibel)", Float) = 1.0
        _Sensitivity ("Linear Sensitivity", Float) = 25.0
        _RangeDecibels ("Decibel Range", Float) = 30.0
        _FieldPeak ("Field Peak", Float) = 1.0

        _IdleOpacity ("Idle Opacity", Range(0, 1)) = 0.04
        _PositiveOpacity ("Positive Lobe Opacity", Range(0, 1)) = 0.55
        _NegativeOpacity ("Negative Lobe Opacity", Range(0, 1)) = 0.95

        _GridIntensity ("Grid", Range(0, 1)) = 0.2
        _ContourSpacing ("Contour Spacing (dB)", Float) = 6.0
        _ContourIntensity ("Contours", Range(0, 1)) = 0.6
        _ContourFlow ("Contour Flow", Float) = 0.4
        _NodalIntensity ("Nodal Lines", Range(0, 1)) = 0.9
        _HatchIntensity ("Negative Hatch", Range(0, 1)) = 0.35

        _HeadForward ("Head Forward", Vector) = (0, 0, 1, 0)

        [HDR] _BaseColor ("Idle Color", Color) = (0.02, 0.1, 0.3, 1.0)
        [HDR] _LowColor ("Low Freq Color", Color) = (1.0, 0.05, 0.0, 1.0)
        [HDR] _MidColor ("Mid Freq Color", Color) = (0.1, 1.0, 0.3, 1.0)
        [HDR] _HighColor ("High Freq Color", Color) = (0.0, 0.8, 1.0, 1.0)
        [HDR] _NegativeRimColor ("Negative Rim Color", Color) = (0.75, 0.25, 1.0, 1.0)
        [HDR] _NodalColor ("Nodal Line Color", Color) = (1.0, 0.92, 0.7, 1.0)
    }
    SubShader
    {
        // one queue behind AmbisonicVisualizerOccluder: its depth lets the formed lobes hide what lies behind them
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "AmbisonicVisualizer.cginc"

            float _IdleOpacity;
            float _PositiveOpacity;
            float _NegativeOpacity;

            float _GridIntensity;
            float _ContourSpacing;
            float _ContourIntensity;
            float _ContourFlow;
            float _NodalIntensity;
            float _HatchIntensity;

            float4 _HeadForward;

            float4 _BaseColor;
            float4 _LowColor;
            float4 _MidColor;
            float4 _HighColor;
            float4 _NegativeRimColor;
            float4 _NodalColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 direction : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (float3 normal : NORMAL)
            {
                v2f o;

                float3 direction = normalize(normal);
                float3 position = SurfacePosition(direction, FieldLevel(EvaluateField(direction)));

                o.pos = OverlayClipPosition(position);
                o.direction = direction;
                o.worldPos = mul(unity_ObjectToWorld, float4(position, 1.0)).xyz;

                return o;
            }

            // anti-aliased line at the integers of t
            float Isoline(float t, float width)
            {
                float distance = abs(frac(t + 0.5) - 0.5) / max(fwidth(t), 1e-6);
                return saturate(width - distance);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // the field is re-evaluated per pixel: lobes, sign and nodal lines stay exact between vertices
                float3 direction = normalize(i.direction);
                float4 field = EvaluateField(direction);

                float3 bands = abs(field.xyz);
                float magnitude = length(field.xyz);
                float level = FieldLevel(field);
                float presence = Presence(level);

                // energy-weighted sign across bands: +1 all positive, -1 all negative
                float negativity = saturate(-dot(field.xyz, bands) / max(dot(field.xyz, field.xyz), 1e-12));

                float3 positive = max(field.xyz, 0.0);
                float3 bandColor = (positive.x * _LowColor.rgb + positive.y * _MidColor.rgb + positive.z * _HighColor.rgb) / max(bands.x + bands.y + bands.z, 1e-9);

                float3 normal = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                normal = dot(normal, viewDir) < 0.0 ? -normal : normal;

                float facing = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - facing, 3.0);
                float diffuse = 0.35 + 0.65 * saturate(dot(normal, normalize(viewDir + float3(0.0, 0.7, 0.0))));

                // positive lobes glow in their band colors; negative lobes are obsidian with a lit rim and hatching
                float3 positiveShade = bandColor * (diffuse * (0.3 + 0.9 * level) + 1.6 * fresnel);
                float hatch = Isoline(dot(direction, float3(0.577, 0.577, 0.577)) * 36.0, 1.2);
                float3 negativeShade = _NegativeRimColor.rgb * (1.8 * fresnel + 0.1 * diffuse + _HatchIntensity * hatch);

                float3 color = lerp(positiveShade, negativeShade, negativity);
                color = lerp(_BaseColor.rgb * diffuse, color, presence);

                float alpha = lerp(_IdleOpacity, lerp(_PositiveOpacity, _NegativeOpacity, negativity), presence);

                // decibel contours drifting outward from the peak
                float contour = Isoline(Decibels(magnitude) / _ContourSpacing + _Time.y * _ContourFlow, 1.0) * _ContourIntensity * presence;
                color += contour * lerp(bandColor * 1.5 + 0.15, _NegativeRimColor.rgb, negativity);
                alpha = max(alpha, contour);

                // nodal cage: zero crossings of the broadband field
                float signedField = field.x + field.y + field.z;
                float nodal = saturate(1.5 - abs(signedField) / max(fwidth(signedField), 1e-12)) * _NodalIntensity * step(1e-9, _FieldPeak);
                color = lerp(color, _NodalColor.rgb, nodal);
                alpha = max(alpha, nodal);

                // world latitude / longitude reference, faded at the poles
                float2 sphereUV = float2(atan2(direction.z, direction.x) * (0.5 / UNITY_PI) + 0.5, acos(clamp(direction.y, -1.0, 1.0)) / UNITY_PI) * 24.0;
                float poleFade = smoothstep(0.02, 0.15, sphereUV.y / 24.0) * smoothstep(0.98, 0.85, sphereUV.y / 24.0);
                float grid = max(Isoline(sphereUV.y, 0.8), Isoline(sphereUV.x, 0.8) * poleFade) * _GridIntensity;
                color += grid * lerp(_BaseColor.rgb * 3.0, bandColor, presence);
                alpha = max(alpha, grid * 0.6);

                // head-forward reticle
                float angle = acos(clamp(dot(direction, _HeadForward.xyz), -1.0, 1.0));
                float reticle = max(saturate(1.5 - abs(angle - 0.07) / max(fwidth(angle), 1e-6)), angle < 0.015 ? 1.0 : 0.0);
                color = lerp(color, _NodalColor.rgb, reticle);
                alpha = max(alpha, reticle);

                return fixed4(color, saturate(alpha));
            }
            ENDCG
        }
    }
}
