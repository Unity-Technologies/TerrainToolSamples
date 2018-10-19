using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class BridgeTool : TerrainPaintTool<BridgeTool>
    {
        Terrain m_StartTerrain = null;
        private Vector3 m_StartPoint;

        Material m_Material = null;
        Material GetPaintMaterial() {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/SetExactHeight"));
            return m_Material;
        }

        [SerializeField]
        AnimationCurve widthProfile = AnimationCurve.Linear(0, 1, 1, 1);

        [SerializeField]
        AnimationCurve heightProfile = AnimationCurve.Linear(0, 0, 1, 0);

        [SerializeField]
        AnimationCurve strengthProfile = AnimationCurve.Linear(0, 1, 1, 1);

        [SerializeField]
        AnimationCurve jitterProfile = AnimationCurve.Linear(0, 0, 1, 0);

        [SerializeField]
        float m_Spacing = 0.01f;

        GUIContent widthProfileContent = new GUIContent("Width Profile", "A multiplier that controls the width of the bridge over the length of the stroke");
        GUIContent heightProfileContent = new GUIContent("Height Offset Profile", "Adds a height offset to the bridge along the length of the stroke (World Units)");
        GUIContent strengthProfileContent = new GUIContent("Strength Profile", "A multiplier that controls influence of the bridge along the length of the stroke");
        GUIContent jitterProfileContent = new GUIContent("Horizontal Offset Profile", "Adds an offset perpendicular to the stroke direction (World Units)");
        
        public override string GetName()
        {
            return "Utility/Bridge";
        }

        public override string GetDesc()
        {
            return "Shift + Click to Set the start point, click to connect the bridge.";
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain,
                                                              editContext.brushTexture,
                                                              editContext.brushSize);

        }
        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            //"Controls the width of the bridge over the length of the stroke"
            widthProfile = EditorGUILayout.CurveField(widthProfileContent, widthProfile);
            heightProfile = EditorGUILayout.CurveField(heightProfileContent, heightProfile);
            strengthProfile = EditorGUILayout.CurveField(strengthProfileContent, strengthProfile);
            jitterProfile = EditorGUILayout.CurveField(jitterProfileContent, jitterProfile);

            m_Spacing = EditorGUILayout.Slider(new GUIContent("Brush Spacing", "Distance between brush splats"), m_Spacing, 1.0f, 100.0f);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        private Vector2 transformToWorld(Terrain t, Vector2 uvs)
        {
            Vector3 tilePos = t.GetPosition();
            return new Vector2(tilePos.x, tilePos.z) + uvs * new Vector2(t.terrainData.size.x, t.terrainData.size.z);
        }

        private Vector2 transformToUVSpace(Terrain originTile, Vector2 worldPos) {
            Vector3 originTilePos = originTile.GetPosition();
            Vector2 uvPos = new Vector2((worldPos.x - originTilePos.x) / originTile.terrainData.size.x,
                                        (worldPos.y - originTilePos.z) / originTile.terrainData.size.z);
            return uvPos;
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            Vector2 uv = editContext.uv;

            //grab the starting position & height
            if (Event.current.shift)
            {
                float height = terrain.terrainData.GetInterpolatedHeight(uv.x, uv.y) / terrain.terrainData.size.y;
                m_StartPoint = new Vector3(uv.x, uv.y, height);
                m_StartTerrain = terrain;
                return true;
            }

            if (!m_StartTerrain || (Event.current.type == EventType.MouseDrag)) {
                return true;
            }

            //get the target position & height
            float targetHeight = terrain.terrainData.GetInterpolatedHeight(uv.x, uv.y) / terrain.terrainData.size.y;
            Vector3 targetPos = new Vector3(uv.x, uv.y, targetHeight);

            if (terrain != m_StartTerrain) {
                //figure out the stroke vector in uv,height space
                Vector2 targetWorld = transformToWorld(terrain, uv);
                Vector2 targetUVs = transformToUVSpace(m_StartTerrain, targetWorld);
                targetPos.x = targetUVs.x;
                targetPos.y = targetUVs.y;
            }

            Vector3 stroke = targetPos - m_StartPoint;
            float strokeLength = stroke.magnitude;
            int numSplats = (int)(strokeLength / (0.001f * m_Spacing));

            Terrain currTerrain = m_StartTerrain;
            Material mat = GetPaintMaterial();

            Vector2 posOffset = new Vector2(0.0f, 0.0f);
            Vector2 currUV = new Vector2();
            Vector4 brushParams = new Vector4();

            Vector2 jitterVec = new Vector2(-stroke.z, stroke.x); //perpendicular to stroke direction
            jitterVec.Normalize();

            for (int i = 0; i < numSplats; i++)
            {
                float pct = (float)i / (float)numSplats;

                float widthScale = widthProfile.Evaluate(pct);
                float heightOffset = heightProfile.Evaluate(pct) / currTerrain.terrainData.size.y;
                float strengthScale = strengthProfile.Evaluate(pct);
                float jitterOffset = jitterProfile.Evaluate(pct) / Mathf.Max(currTerrain.terrainData.size.x, currTerrain.terrainData.size.z);

                Vector3 currPos = m_StartPoint + pct * stroke;

                //add in jitter offset (needs to happen before tile correction)
                currPos.x += posOffset.x + jitterOffset * jitterVec.x;
                currPos.y += posOffset.y + jitterOffset * jitterVec.y;

                if (currPos.x >= 1.0f && (currTerrain.rightNeighbor != null)) {
                    currTerrain = currTerrain.rightNeighbor;
                    currPos.x -= 1.0f;
                    posOffset.x -= 1.0f;
                }
                if(currPos.x <= 0.0f && (currTerrain.leftNeighbor != null)) {
                    currTerrain = currTerrain.leftNeighbor;
                    currPos.x += 1.0f;
                    posOffset.x += 1.0f;
                }
                if(currPos.y >= 1.0f && (currTerrain.topNeighbor != null)) {
                    currTerrain = currTerrain.topNeighbor;
                    currPos.y -= 1.0f;
                    posOffset.y -= 1.0f;
                }
                if(currPos.y <= 0.0f && (currTerrain.bottomNeighbor != null)) {
                    currTerrain = currTerrain.bottomNeighbor;
                    currPos.y += 1.0f;
                    posOffset.y += 1.0f;
                }

                currUV.x = currPos.x;
                currUV.y = currPos.y;

                int finalBrushSize = (int)(widthScale * (float)editContext.brushSize);
                float finalHeight =  (m_StartPoint + pct * stroke).z + heightOffset;

                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(currTerrain, currUV, finalBrushSize, 0.0f);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(currTerrain, brushXform.GetBrushXYBounds());
                
                mat.SetTexture("_BrushTex", editContext.brushTexture);

                brushParams.x = editContext.brushStrength * strengthScale;
                brushParams.y = 0.5f * finalHeight;

                mat.SetVector("_BrushParams", brushParams);

                TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

                Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

                TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Bridge");
            }
            return false;
        }
    }
}
