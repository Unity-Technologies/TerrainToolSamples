using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class SmudgeHeightTool : TerrainPaintTool<SmudgeHeightTool>
    {
        EventType m_PreviousEvent = EventType.Ignore;
        Vector2 m_PrevBrushPos = new Vector2(0.0f, 0.0f);

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerrainToolSamples/SmudgeHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Transform/Smudge Height";
        }

        public override string GetDesc()
        {
            return "Click to Smudge the terrain height in the direction of the brush stroke.";
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
            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                m_PrevBrushPos = editContext.uv;
                return false;
            }
            
            if (Event.current.type == EventType.MouseDrag && m_PreviousEvent == EventType.MouseDrag) 
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

                Vector2 smudgeDir = editContext.uv - m_PrevBrushPos;

                paintContext.sourceRenderTexture.filterMode = FilterMode.Bilinear;

                Material mat = GetPaintMaterial();
                Vector4 brushParams = new Vector4(editContext.brushStrength, smudgeDir.x, smudgeDir.y, 0);
                mat.SetTexture("_BrushTex", editContext.brushTexture);
                mat.SetVector("_BrushParams", brushParams);
                TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
                Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

                TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smudge Height");

                m_PrevBrushPos = editContext.uv;
            }
            m_PreviousEvent = Event.current.type;
            return false;
        }
    }
}
