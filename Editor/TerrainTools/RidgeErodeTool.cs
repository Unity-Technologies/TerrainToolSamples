using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class RidgeErodeTool : TerrainPaintTool<RidgeErodeTool>
    {
        float m_ErosionStrength = 16.0f;

        [SerializeField]
        float m_MixStrength = 0.7f;

        Material m_Material = null;

        Material GetPaintMaterial()
        {
            if (m_Material == null)
				m_Material = new Material(Shader.Find("TerrainToolSamples/RidgeErode"));
            return m_Material;
        }

        public override string GetName()
        {
			return "Erosion/Ridging Erosion";
        }

        public override string GetDesc()
        {
            return "Click to erode";
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(
                terrain,
                editContext.brushTexture,
                editContext.brushSize);
        }

		public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
			EditorGUI.BeginChangeCheck();
//			m_ErosionStrength = EditorGUILayout.Slider(new GUIContent("Erosion strength"), m_ErosionStrength, 1, 128.0f);
			m_MixStrength = EditorGUILayout.Slider(new GUIContent("Feature Sharpness"), m_MixStrength, 0, 1);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
				Save(true);
		}
		
		public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

            Material mat = GetPaintMaterial();

            // apply brush
			Vector4 brushParams = new Vector4(
                editContext.brushStrength,
                m_ErosionStrength,
                m_MixStrength,
                0.0f);

            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Ridge Erode");

            return false;
        }
    }
}
