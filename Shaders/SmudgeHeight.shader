    Shader "TerrainToolSamples/SmudgeHeight" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_SMUDGE_X      (_BrushParams[1])
            #define BRUSH_SMUDGE_Y      (_BrushParams[2])
            #define BRUSH_ROTATION      (_BrushParams[3])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                return o;
            }

        ENDCG


        Pass    // 11 Smudge
        {
            Name "Smudge Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SmudgeHeight

            float4 SmudgeHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float brushStrength = oob * BRUSH_STRENGTH * 10.0f;
                float blendValue = UnpackHeightmap(tex2D(_BrushTex, brushUV));

                float2 smudgedUVs = heightmapUV - brushStrength * float2(BRUSH_SMUDGE_X, BRUSH_SMUDGE_Y);

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                
                float h = UnpackHeightmap(tex2D(_MainTex, smudgedUVs));

                return PackHeightmap(lerp(height, h, blendValue));
            }
            ENDCG
        }
    }
    Fallback Off
}
