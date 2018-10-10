using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class NoiseBrushTool : TerrainPaintTool<NoiseBrushTool>
    {
        public enum NoiseType
        {
            Perlin = 0,
            Brownian,
            Worley,
        }

        [SerializeField] private Texture2D m_noiseTex;
        [SerializeField] private float m_detailSize = 16.0f;
        [SerializeField] private bool m_usePosAsSeed = false;
        
        private Vector3 m_worldPos;
        private NoiseType m_noiseType;

        private static Material m_material;
        private static Material material
        {
            get
            {
                if(m_material == null)
                {
                    m_material = new Material(Shader.Find("Hidden/TerrainTools/NoiseBrush"));
                }

                return m_material;
            }
        }

        public override string GetName()
        {
            return "Utility/Noise Brush";
        }

        public override string GetDesc()
        {
            return "Left click to raise.\n\nShift and Left click to lower";
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            float detailSize = EditorGUILayout.Slider(Styles.detailSizeContent, m_detailSize, 1.0f, 100.0f);
            bool usePosAsSeed = EditorGUILayout.Toggle(Styles.usePosAsSeedContent, m_usePosAsSeed);
            NoiseType noiseType = (NoiseType)EditorGUILayout.EnumPopup(Styles.noiseTypeContent, m_noiseType);
            Texture2D tex = EditorGUILayout.ObjectField(Styles.noiseTextureContent, m_noiseTex, typeof(Texture2D), false) as Texture2D;

            editContext.ShowBrushesGUI(0);

            if(EditorGUI.EndChangeCheck())
            {
                m_noiseTex = tex;
                if(m_noiseTex == null) m_noiseTex = Textures.defaultNoise;

                m_detailSize = detailSize;
                m_usePosAsSeed = usePosAsSeed;
                m_noiseType = noiseType;
                
                Save(true);
            }
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform)
        {
            brushStrength = Event.current.shift ? -brushStrength : brushStrength;
            Vector4 brushParams = new Vector4(0.01f * brushStrength, m_detailSize, 0.0f, 0.0f);
            Vector4 noiseParams = new Vector4(m_worldPos.x, m_worldPos.z, m_usePosAsSeed ? 1.0f : 0.0f, 0.0f);
            material.SetTexture("_NoiseTex", m_noiseTex);
            material.SetTexture("_BrushTex", brushTexture);
            material.SetVector("_BrushParams", brushParams);
            material.SetVector("_NoiseParams", noiseParams);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, material);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, material, 0);
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                m_worldPos = editContext.raycastHit.point;
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

                Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

                TerrainPaintUtilityEditor.DrawBrushPreview(paintContext,
                                    TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture,
                                    editContext.brushTexture, brushXform, material, 0);

                // draw result preview
                {
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                    TerrainPaintUtilityEditor.DrawBrushPreview(paintContext,
                                    TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture,
                                    editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Noise");
            return true;
        }

        static class Textures
        {
            public static Texture2D defaultNoise;

            static Textures()
            {
                defaultNoise = Resources.Load("terrainToolNoise", typeof(Texture2D)) as Texture2D;
            }
        }

        static class Styles
        {
            public static GUIContent noiseTextureContent = new GUIContent("Noise Texture:",
                                                "Noise texture to sample when applying noise");
            public static GUIContent detailSizeContent = new GUIContent("Detail Size:",
                                                "Larger value will enhance larger features, smaller values will enhance smaller features");
            public static GUIContent usePosAsSeedContent = new GUIContent("Use PosXZ as Seed:",
                                                "Opt to use the XZ world position as a seed for the noise");
            public static GUIContent noiseTypeContent = new GUIContent("Noise Type:");
        }
    }
}