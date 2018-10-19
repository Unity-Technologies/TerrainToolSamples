using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class ErodeHeightTool : TerrainPaintTool<ErodeHeightTool>
    {
        [SerializeField]
        float m_FeatureSize;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/ErodeHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Erosion/Erode Height (Simple)";
        }

        public override string GetDesc()
        {
            return "Click to erode the terrain height away from the local maxima.";
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
            m_FeatureSize = EditorGUILayout.Slider(new GUIContent("Detail Size", "Larger value will affect larger features, smaller values will affect smaller features"), m_FeatureSize, 1.0f, 100.0f);

            editContext.ShowBrushesGUI(0);
            
            if (EditorGUI.EndChangeCheck())
                Save(true);


        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

            paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, 0.0f, 0.1f * m_FeatureSize, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Erode Height");
            return false;
        }
    }
}
