Shader "TerrainToolSamples/RidgeErode"
{
    Properties 
	{
	 _MainTex ("Texture", any) = "" {} 
	}

    SubShader {

        ZTest Always Cull Off ZWrite Off

        Pass
        {
            Name "Ridge Erode"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment Erode

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH     (_BrushParams[0])
			#define EROSION_STRENGTH   (_BrushParams[1])
			#define MIX_STRENGTH	   (_BrushParams[2])

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

			float4 Erode(v2f i) : SV_Target
            {
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

				const float2 coords [4] = { {-1,0}, { 1,0}, {0, -1}, { 0, 1} };

				float hc = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float hl = UnpackHeightmap(tex2D(_MainTex, heightmapUV + coords[0] * _MainTex_TexelSize.xy));
				float hr = UnpackHeightmap(tex2D(_MainTex, heightmapUV + coords[1] * _MainTex_TexelSize.xy));
				float ht = UnpackHeightmap(tex2D(_MainTex, heightmapUV + coords[2] * _MainTex_TexelSize.xy));
				float hb = UnpackHeightmap(tex2D(_MainTex, heightmapUV + coords[3] * _MainTex_TexelSize.xy));

				float l = min(hl, hr);
				float r = max(hl, hr);
				float b = min(hb, ht);
				float t = max(hb, ht);

				float height = hc;
					
				if (height > l && height < r)
				{
					float hlr01 = pow((height - l) / (r - l), EROSION_STRENGTH);
					height = hlr01 * (r - l) + l;
				}	

				if (height > b && height < t)
				{
					float hbt01 = pow((height - b) / (t - b), EROSION_STRENGTH);
					height = hbt01 * (t - b) + b;
				}
				
				height = lerp(0.25f * ( hl + hr + ht + hb ), height, MIX_STRENGTH);
				
				// out of bounds multiplier
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				float brushStrength = oob * BRUSH_STRENGTH * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				return clamp(lerp(hc, height, brushStrength), 0, 0.5f);
            }
            ENDCG
        }
    }
    Fallback Off
}
