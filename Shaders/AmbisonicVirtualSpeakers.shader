Shader "Hidden/WyrmAudio/AmbisonicVirtualSpeakers"
{
    Properties
    {
        _SpeakerRadius ("Speaker Radius", Float) = 4.0
        _SpeakerSize ("Speaker Size", Float) = 0.12
        _SpeakerRange ("Level Range (dB)", Float) = 36.0
        _SpeakerIdleOpacity ("Idle Opacity", Range(0, 1)) = 0.15

        [HDR] _BaseColor ("Idle Color", Color) = (0.02, 0.1, 0.3, 1.0)
        [HDR] _LowColor ("Low Freq Color", Color) = (1.0, 0.05, 0.0, 1.0)
        [HDR] _MidColor ("Mid Freq Color", Color) = (0.1, 1.0, 0.3, 1.0)
        [HDR] _HighColor ("High Freq Color", Color) = (0.0, 0.8, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" }

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

            // must match VirtualSpeakerLayout.COUNT
            #define SPEAKER_COUNT 240

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 direction : TEXCOORD0;
                float2 index : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 color : TEXCOORD0;
                float alpha : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            // xyz: low, mid, high decoder feeds (signed)
            float4 _SpeakerFeeds[SPEAKER_COUNT];
            float _SpeakerPeak;

            float _SpeakerRadius;
            float _SpeakerSize;
            float _SpeakerRange;
            float _SpeakerIdleOpacity;

            float4 _BaseColor;
            float4 _LowColor;
            float4 _MidColor;
            float4 _HighColor;

            v2f vert (appdata v)
            {
                v2f o;

                float3 feed = _SpeakerFeeds[(int)(v.index.x + 0.5)].xyz;
                float magnitude = length(feed);

                // level relative to the loudest speaker, mapped over the dB range
                float level = saturate(1.0 + 20.0 * log10(max(magnitude, 1e-12) / max(_SpeakerPeak, 1e-12)) / _SpeakerRange);

                // only positive band feeds carry color: negative (phase-inverted) feeds render black
                float3 bands = abs(feed);
                float3 positive = max(feed, 0.0);
                float3 signedColor = (positive.x * _LowColor.rgb + positive.y * _MidColor.rgb + positive.z * _HighColor.rgb) / (bands.x + bands.y + bands.z + 1e-12);

                o.color = lerp(_BaseColor.rgb, signedColor, level > 0.0 ? 1.0 : 0.0);
                o.alpha = lerp(_SpeakerIdleOpacity, 1.0, level);

                float4 position = float4(v.direction * _SpeakerRadius + v.vertex.xyz * (_SpeakerSize * (0.3 + 0.7 * level)), 1.0);

                o.pos = UnityObjectToClipPos(position);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(position));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
                return fixed4(i.color * (0.55 + 0.45 * facing), i.alpha);
            }
            ENDCG
        }
    }
}
