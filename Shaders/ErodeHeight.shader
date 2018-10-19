    Shader "TerrainToolSamples/ErodeHeight" {

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
            #define BRUSH_FEATURESIZE   (_BrushParams[2])
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


        Pass    // 11 Erode
        {
            Name "Erode Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment ErodeHeight

            float4 ErodeHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushStrength = oob * BRUSH_STRENGTH * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                float avg = 0.0F;
                float xoffset = _MainTex_TexelSize.x * BRUSH_FEATURESIZE;
                float yoffset = _MainTex_TexelSize.y * BRUSH_FEATURESIZE;

                float localMaxima = height;

                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset,  0      ))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset,  0      ))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset,  yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset,  yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset, -yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset, -yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( 0,        yoffset))));
                localMaxima = max(localMaxima, UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( 0,       -yoffset))));

                float sharpness = 0.8F;
                float erodeAmt = pow(clamp(0.01F * (localMaxima - height), 0.0f, 1.0f), sharpness);
                float h = height - erodeAmt;

                return PackHeightmap(lerp(height, h, brushStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
