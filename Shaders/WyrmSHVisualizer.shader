Shader "Hidden/WyrmAudio/SHVisualizer"
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0; // Restored UV
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 mappedColor : TEXCOORD0;
                float intensity : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 objPos : TEXCOORD4; 
                float2 uv : TEXCOORD5; // Restored UV
                float bandOpacity : TEXCOORD6;
            };

            float4 _SHCoeffs[16];
            
            float _BaseRadius;
            float _DeformScale;
            float _Sensitivity;
            float _IdleOpacity;
            float _GridIntensity;
            
            float4 _BaseColor;
            float4 _LowColor;
            float4 _MidColor;
            float4 _HighColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv; // Pass UV to fragment
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.objPos = normalize(v.normal); 
                
                float3 u = normalize(v.normal);
                float x = u.z;
                float y = -u.x;
                float z = u.y;

                float b[16];
                b[0] = 0.28209479;
                b[1] = 0.48860251 * y;
                b[2] = 0.48860251 * z;
                b[3] = 0.48860251 * x;
                b[4] = 1.0925484 * x * y;
                b[5] = 1.0925484 * y * z;
                b[6] = 0.3153915 * (3.0 * z * z - 1.0);
                b[7] = 1.0925484 * x * z;
                b[8] = 0.5462742 * (x * x - y * y);
                b[9]  = 0.5900435 * y * (3.0 * x * x - y * y);
                b[10] = 2.8906114 * x * y * z;
                b[11] = 0.4570457 * y * (5.0 * z * z - 1.0);
                b[12] = 0.3731763 * z * (5.0 * z * z - 3.0);
                b[13] = 0.4570457 * x * (5.0 * z * z - 1.0);
                b[14] = 1.4453057 * z * (x * x - y * y);
                b[15] = 0.5900435 * x * (x * x - 3.0 * y * y);

                float4 evaluatedSH = float4(0,0,0,0);
                for(int i = 0; i < 16; i++)
                {
                    evaluatedSH += b[i] * _SHCoeffs[i];
                }

                if (isnan(evaluatedSH.w) || isinf(evaluatedSH.w)) evaluatedSH = float4(0,0,0,0);
                evaluatedSH *= _Sensitivity;

                // Use abs() to reveal full SH lobes
                float3 bands = abs(evaluatedSH.rgb);

                float totalBandEnergy = bands.x + bands.y + bands.z + 0.0001; 
                
                o.mappedColor =
                    (bands.x * _LowColor.rgb +
                    bands.y * _MidColor.rgb +
                    bands.z * _HighColor.rgb) / totalBandEnergy;

                o.bandOpacity =
                    (bands.x * _LowColor.a +
                    bands.y * _MidColor.a +
                    bands.z * _HighColor.a) / totalBandEnergy;

                float avgEQ = dot(bands, float3(0.3333, 0.3333, 0.3333));
                float maxEQ = max(bands.x, max(bands.y, bands.z));
                
                o.intensity = lerp(avgEQ, maxEQ, 0.5); 

                float finalRadius = _BaseRadius + (pow(o.intensity, 0.8) * _DeformScale);
                v.vertex.xyz = u * finalRadius;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float audioGlow = saturate(i.intensity);
                float3 baseColor = lerp(_BaseColor.rgb, i.mappedColor, audioGlow);

                float2 gridUV = frac(i.uv * 24.0);
                float horizontalLines = smoothstep(0.95, 1.0, gridUV.y);
                float verticalLines = smoothstep(0.95, 1.0, gridUV.x);
                
                float poleFade = smoothstep(0.02, 0.15, i.uv.y) * smoothstep(0.98, 0.85, i.uv.y);
                verticalLines *= poleFade; 
                
                float gridLine = saturate(horizontalLines + verticalLines);
                float dynamicGridIntensity = _GridIntensity * (0.2 + audioGlow * 1.5);
                gridLine *= dynamicGridIntensity; 

                float pulse = sin(i.objPos.y * 20.0 - _Time.y * 15.0) * 0.5 + 0.5;
                float pulseGlow = pulse * audioGlow * 0.5;

                float NdotV = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
                float rim = 1.0 - NdotV;
                float rimGlow = smoothstep(0.5, 1.0, rim) * (0.1 + audioGlow * 1.5); 
                
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