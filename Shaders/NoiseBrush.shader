Shader "Hidden/TerrainTools/NoiseBrush"
{
    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader
    {
        ZTest Always Cull OFF ZWrite Off

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "TerrainTool.cginc"

        texture2D _MainTex;
        SamplerState sampler_MainTex;

        float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

        sampler2D _BrushTex;

        float4 _BrushParams;
        #define BRUSH_STRENGTH      (_BrushParams[0])
        #define DETAIL_SIZE         (_BrushParams[1])

        float4 _NoiseParams;
        #define NOISE_SEED_XZ       (_NoiseParams.xy)
        #define USE_NOISE_SEED      (_NoiseParams.z)
        #define NOISE_OFFSET        (NOISE_SEED_XZ * USE_NOISE_SEED)

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 pcUV : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 pcUV : TEXCOORD0;
        };

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

        v2f vert(appdata_t v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.pcUV = v.pcUV;
            return o;
        }

        ENDHLSL

        Pass
        {
            Name "Noise Brush"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment NoiseFrag

            sampler2D _NoiseTex;

            float noise(float2 p)
            {
                return tex2D(_NoiseTex, 0.1 * p).r;
            }

            float fbm(float2 p)
            {
                const float2x2 m = float2x2(0.8, 0.6, -0.6, 0.8);

                float f = 0.0;
				f += 0.5000 * noise(p);
                p = mul(m, p)*2.02;
				f += 0.2500 * noise(p);
                p = mul(m, p)*2.03;
				f += 0.1250 * noise(p);
                p = mul(m, p)*2.01;
				f += 0.0625 * noise(p);
                p = mul(m, p)*2.04;
				f += 0.0312 * noise(p);
                
				return f / 0.9375;
            }

            float4 NoiseFrag(  v2f i ) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(_MainTex.Sample(sampler_MainTex, heightmapUV));
                float brushShape = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
                float noise = fbm(heightmapUV * DETAIL_SIZE + NOISE_OFFSET);

                return PackHeightmap(clamp(height + BRUSH_STRENGTH * noise * brushShape, 0, 0.5f));
            }

            ENDHLSL
        }
    }
}