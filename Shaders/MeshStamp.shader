    Shader "TerrainToolSamples/MeshStamp" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            
            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f defVert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

        ENDCG

        
        Pass    // 0 - Draw Mesh Preview Front
        {
            ZTest LEqual Cull Back

            Name "Draw Mesh Stamp Preview - Front Faces"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4x4 _RotMatrix;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.texcoord = clamp(abs(mul(_RotMatrix, v.vertex).y), 0.0f, 1.0f);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return lerp(float4(0.5f, 0.5f, 1.0f, 0.65f), float4(0.5f, 0.5f, 1.0f, 0.95f), i.texcoord.x * 1.5f);
            }

            ENDCG
        }

        Pass    // 1 - Draw Mesh Preview Back
        {
            ZTest GEqual Cull Front

            Name "Draw Mesh Stamp Preview - Back Faces"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4x4 _RotMatrix;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.texcoord = clamp(abs(mul(_RotMatrix, v.vertex).y), 0.0f, 1.0f);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return lerp(float4(1.0f, 0.5f, 0.5f, 0.5f), float4(1.0f, 0.5f, 0.5f, 0.95f), i.texcoord.x * 1.5f);
            }

            ENDCG
        }

        
        Pass    // 2 Mesh Stamp - Depth Pass (front faces)
        {
            ZTest LEqual
            Cull Back
            ZWrite On

            Name "Mesh Stamp - Depth Render (front faces)"
            CGPROGRAM
            #pragma vertex GatherHeightVert
            #pragma fragment GatherHeightFrag

            float4x4 _Model;
            float4x4 _MVP;
            float4 _StampParams;

            #define StampTerrainHeight _StampParams[0]

            struct StampMeshVertexOutput
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            StampMeshVertexOutput GatherHeightVert(appdata_base v)
            {
                StampMeshVertexOutput o;
                o.pos = mul(_MVP, v.vertex);
                o.worldPos = mul(_Model, v.vertex);
                return o;
            }

            float4 GatherHeightFrag(StampMeshVertexOutput i) : SV_TARGET
            {
                return PackHeightmap(clamp(i.worldPos.z / StampTerrainHeight, 0.0f, 0.5f));
            }
            ENDCG
        }

        Pass    // 3 Mesh Stamp - Depth Pass (back faces)
        {
            ZTest GEqual
            Cull Front
            ZWrite On

            Name "Mesh Stamp - Depth Render (back faces)"
            CGPROGRAM
            #pragma vertex GatherHeightVert
            #pragma fragment GatherHeightFrag

            float4x4 _Model;
            float4x4 _MVP;
            float4 _StampParams;

            #define StampTerrainHeight _StampParams[0]

            struct StampMeshVertexOutput
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            StampMeshVertexOutput GatherHeightVert(appdata_base v)
            {
                StampMeshVertexOutput o;
                o.pos = mul(_MVP, v.vertex);
                o.worldPos = mul(_Model, v.vertex);
                return o;
            }

            float4 GatherHeightFrag(StampMeshVertexOutput i) : SV_TARGET
            {
                return PackHeightmap(clamp(i.worldPos.z / StampTerrainHeight, 0.0f, 0.5f));
            }
            ENDCG
        }

        Pass    // 4 Mesh Stamp to Heightmap
        {
            Name "Mesh Stamp to Heightmap"
            CGPROGRAM
            #pragma vertex defVert
            #pragma fragment StampMesh

            sampler2D _MeshStampTex;
            float4 _StampParams;
            float4 _BrushParams;

            #define StampTerrainHeight  _StampParams[0]
            #define MeshHeight          _StampParams[1]
            #define OverwriteMode       _StampParams[2]
            #define Addition            _StampParams[3]
            #define HeightDelta         _BrushParams[0]

            float4 StampMesh(v2f i) : SV_Target
            {
                float stampedHeight = UnpackHeightmap(tex2D(_MeshStampTex, i.texcoord));
                float currentHeight = UnpackHeightmap(tex2D(_MainTex, i.texcoord));

                // get adjusted mesh height (works for both addition and subtraction)
                if (OverwriteMode <= 0.0f)
                {
                    float meshHeightInTerrainUnits = MeshHeight / StampTerrainHeight;
                    float tempStampedHeight = stampedHeight - meshHeightInTerrainUnits;
                    float tempCurrentHeight = currentHeight - meshHeightInTerrainUnits;

                    float newHeight = tempStampedHeight + tempCurrentHeight;
                    float deltaAdjustment = HeightDelta / StampTerrainHeight;
                    newHeight += meshHeightInTerrainUnits + deltaAdjustment;
                    stampedHeight = clamp(newHeight, 0.0f, 0.5f);
                }

                // addition must not subtract terrain
                // subtraction cannot add terrain
                if ((Addition > 0.0f && currentHeight < stampedHeight) ||
                    (Addition <= 0.0f && stampedHeight < currentHeight))
                    return PackHeightmap(stampedHeight);

                // use original height if it fails the rules
                return PackHeightmap(currentHeight);
            }

            ENDCG
        }

    }
    Fallback Off
}
