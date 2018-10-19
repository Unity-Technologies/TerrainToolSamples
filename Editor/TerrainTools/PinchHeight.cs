using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class PinchHeightTool : TerrainPaintTool<PinchHeightTool>
    {
        [SerializeField]
        float m_PinchAmount = 5.0f;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/PinchHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Transform/Pinch Height";
        }

        public override string GetDesc()
        {
            return "Click to Pinch the terrain height. Click plus shift to bulge.";
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

            m_PinchAmount = EditorGUILayout.Slider(new GUIContent("Pinch Amount", "Negative values bulge, positive values pinch"), m_PinchAmount, -100.0f, 100.0f);
            editContext.ShowBrushesGUI(0);
            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

            float finalPinchAmount = m_PinchAmount * 0.005f; //scale to a reasonable value and negate so default mode is clockwise
            if (Event.current.shift) {
                finalPinchAmount *= -1.0f;
            }

            paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, 0.0f, finalPinchAmount, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Pinch Height");
            return false;
        }
    }
}
