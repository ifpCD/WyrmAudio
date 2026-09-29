Shader "Hidden/WyrmAudio/AmbisonicVisualizerOccluder"
{
    // Depth of the formed lobes, drawn one queue ahead of the field. Offset keeps the field's own surface in front of it.
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Cull Off
        ZWrite On
        ZTest LEqual
        ColorMask 0
        Offset 1, 2

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "AmbisonicVisualizer.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float presence : TEXCOORD0;
            };

            v2f vert (float3 normal : NORMAL)
            {
                float3 direction = normalize(normal);
                float level = FieldLevel(EvaluateField(direction));

                v2f o;
                o.pos = OverlayClipPosition(SurfacePosition(direction, level));
                o.presence = Presence(level);
                return o;
            }

            // the faint base sphere hides nothing
            fixed4 frag (v2f i) : SV_Target
            {
                clip(i.presence - 0.5);
                return 0;
            }
            ENDCG
        }
    }
}
