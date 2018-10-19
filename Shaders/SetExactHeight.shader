    Shader "TerrainToolSamples/SetExactHeight" {

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
            #define BRUSH_PINCHAMOUNT   (_BrushParams[2])
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
            Name "Set Exact Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SetExactHeight

			/*
            float3 RotateUVs(float2 sourceUV, float rotAngle)
            {
                float4 rotAxes;
                rotAxes.x = cos(rotAngle);
                rotAxes.y = sin(rotAngle);
                rotAxes.w = rotAxes.x;
                rotAxes.z = -rotAxes.y;

                float2 tempUV = sourceUV - float2(0.5, 0.5);
                float3 retVal;

                // We fix some flaws by setting zero-value to out of range UVs, so what we do here
                // is test if we're out of range and store the mask in the third component.
                retVal.xy = float2(dot(rotAxes.xy, tempUV), dot(rotAxes.zw, tempUV)) + float2(0.5, 0.5);
                tempUV = clamp(retVal.xy, float2(0.0, 0.0), float2(1.0, 1.0));
                retVal.z = ((tempUV.x == retVal.x) && (tempUV.y == retVal.y)) ? 1.0 : 0.0;
                return retVal;
            }
		*/

            float4 SetExactHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float oldHeight = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				//float brushStrength = saturate(BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV)));
				float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                float targetHeight = BRUSH_TARGETHEIGHT;

                // have to do this check to ensure strength 0 == no change (code below makes a super tiny change even with strength 0)
                /*if (brushStrength > 0.0f)
                {
                    float deltaHeight = height - targetHeight;

                    // see https://www.desmos.com/calculator/880ka3lfkl
                    float p = saturate(brushStrength);
                    float w = (1.0f - p) / (p + 0.000001f);
//                  float w = (1.0f - p*p) / (p + 0.000001f);       // alternative TODO test and compare
                    float fx = clamp(w * deltaHeight, -1.0f, 1.0f);
                    float g = fx * (0.5f * fx * sign(fx) - 1.0f);

                    deltaHeight = deltaHeight + g / w;

                    height = targetHeight + deltaHeight;
                }
				*/

                //return PackHeightmap(saturate(brushStrength) * targetHeight);
				return PackHeightmap(lerp(oldHeight, targetHeight, brushStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
