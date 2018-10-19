using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class TerraceErosion : TerrainPaintTool<TerraceErosion>
    {
        [SerializeField]
        float m_FeatureSize = 150.0f;

        [SerializeField]
        float m_BevelAmountInterior = 0.0f;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/TerraceErosion"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Erosion/Terrace Erosion";
        }

        public override string GetDesc()
        {
            return "Use to terrace terrain";
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
            m_FeatureSize = EditorGUILayout.Slider(new GUIContent("Terrace Count", "Larger value will result in more terraces"), m_FeatureSize, 2.0f, 1000.0f);
            m_BevelAmountInterior = EditorGUILayout.Slider(new GUIContent("Interior Corner Weight", "Amount to retain the original height in each interior corner of the terrace steps"), m_BevelAmountInterior, 0.0f, 1.0f);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, m_FeatureSize, m_BevelAmountInterior, 0.0f);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());

            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Terrace Erosion");
            return false;
        }
    }
}
