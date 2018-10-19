using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class TwistHeightTool : TerrainPaintTool<TwistHeightTool>
    {
        [SerializeField]
        float m_TwistAmount;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/TwistHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Transform/Twist Height";
        }

        public override string GetDesc()
        {
            return "Click to Twist the terrain height.";
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

            m_TwistAmount = EditorGUILayout.Slider(new GUIContent("Twist Amount", "Negative values twist clockwise, Positive values twist counter clockwise"), m_TwistAmount, -100.0f, 100.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);

            editContext.ShowBrushesGUI(0);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

            float finalTwistAmount = m_TwistAmount * -0.001f; //scale to a reasonable value and negate so default mode is clockwise
            if (Event.current.shift)
            {
                finalTwistAmount *= -1.0f;
            }

            paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, 0.0f, finalTwistAmount, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Twist Height");
            return false;
        }
    }
}
