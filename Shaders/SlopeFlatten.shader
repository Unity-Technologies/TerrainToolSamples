    Shader "TerrainToolSamples/SlopeFlatten" {

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
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])
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


        Pass
        {
            Name "Slope Flatten Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SlopeFlattenHeight

            float4 SlopeFlattenHeight(v2f i) : SV_Target
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
                float xyoffset = xoffset * yoffset / sqrt(0.5 * xoffset * xoffset + 0.5 * yoffset * yoffset);

                float x0 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset, 0.0F)));
                float x1 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset, 0.0F)));
                float y0 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-yoffset, 0.0F)));
                float y1 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( yoffset, 0.0F)));

                float x0y0 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset * 0.707F, -yoffset * 0.707F)));
                float x1y0 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset * 0.707F, -yoffset * 0.707F)));
                float x1y1 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset * 0.707F,  yoffset * 0.707F)));
                float x0y1 = UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset * 0.707F,  yoffset * 0.707F)));

                float mx = (x1 - x0) / (xoffset * 2.0F);
                float my = (y1 - y0) / (yoffset * 2.0F);
                float mxy = (x1y1 - x0y0) / (xyoffset * 2.0F);
                float myx = (x1y0 - x0y1) / (xyoffset * 2.0F);

                float target_height = 0.25F * ((x0 + xoffset * mx) + (y0 + yoffset * my) + (x0y0 + xyoffset * mxy) + (x0y1 + xyoffset * myx));

                return PackHeightmap(lerp(height, target_height, brushStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
