    Shader "TerrainToolSamples/TerraceErosion" {

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
            #define BRUSH_FEATURESIZE   (_BrushParams[1])
			#define BRUSH_BEVELINTERIOR (_BrushParams[2])
            #define BRUSH_BEVELEXTERIOR (_BrushParams[3])

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
            Name "Terrace Erosion"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SharpenHeight

            float4 SharpenHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushStrength = oob * BRUSH_STRENGTH * UnpackHeightmap(tex2D(_BrushTex, brushUV));

				float scaledHeight = height * BRUSH_FEATURESIZE;
				float terracedHeight = round(scaledHeight);
				float dh = scaledHeight - terracedHeight;

				if (dh > (1.0 - BRUSH_BEVELINTERIOR)) {
					terracedHeight = scaledHeight;
				}

                return PackHeightmap(lerp (height, terracedHeight / (BRUSH_FEATURESIZE), brushStrength));
                
            }
            ENDCG
        }
    }
    Fallback Off
}
