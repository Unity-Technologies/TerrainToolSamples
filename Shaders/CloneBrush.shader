Shader "TerrainToolSamples/CloneBrush"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }

    SubShader
    {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])
            #define BRUSH_STAMPHEIGHT   (_BrushParams[2])
            #define BRUSH_ROTATION      (_BrushParams[3])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
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
                o.texcoord = v.texcoord;
                return o;
            }

            float SmoothApply(float height, float brushStrength, float targetHeight)
            {
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = height < targetHeight ? height : targetHeight;
                }
                else
                {
                    height -= brushStrength;
                    height = height > targetHeight ? height : targetHeight;
                }
                return height;
            }

            float ApplyBrush(float height, float brushStrength)
            {
                return SmoothApply(height, brushStrength, BRUSH_TARGETHEIGHT);
            }

        ENDCG


        Pass    // 0 clone stamp tool (alphaMap)
        {
            Name "Clone Stamp Tool Alphamap"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CloneAlphamap

            sampler2D _CloneTex;

            float4 CloneAlphamap(v2f i) : SV_Target
            {
                float3 sampleUV = RotateUVs(i.texcoord, BRUSH_ROTATION);

                float currentAlpha = tex2D(_MainTex, i.texcoord).r;
                float sampleAlpha = tex2D(_CloneTex, i.texcoord).r;
                float brushShape = BRUSH_STRENGTH * sampleUV.z * UnpackHeightmap(tex2D(_BrushTex, i.texcoord));

                return SmoothApply(currentAlpha, brushShape, sampleAlpha);
            }
            ENDCG
        }

        Pass    // 1 clone stamp tool (heightmap)
        {
            Name "Clone Stamp Tool Heightmap"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CloneHeightmap

            sampler2D _CloneTex;

            #define HeightOffset     (_BrushParams[1])
            #define TerrainMaxHeight (_BrushParams[2])

            float4 CloneHeightmap(v2f i) : SV_Target
            {
                float3 sampleUV = RotateUVs(i.texcoord, BRUSH_ROTATION);

                float currentHeight = UnpackHeightmap(tex2D(_MainTex, i.texcoord));
                float sampleHeight = UnpackHeightmap(tex2D(_CloneTex, i.texcoord)) + (HeightOffset / TerrainMaxHeight);

                // * 0.5f since strength in this is far more potent than other tools since its not smoothly applied to a target
                float brushShape = BRUSH_STRENGTH * 0.5f * sampleUV.z * UnpackHeightmap(tex2D(_BrushTex, i.texcoord));

                return PackHeightmap(clamp(lerp(currentHeight, sampleHeight, brushShape), 0.0f, 0.5f));
            }
            ENDCG
        }
    }
    Fallback Off
}
