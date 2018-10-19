using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class CloneBrushTool : TerrainPaintTool<CloneBrushTool>
    {
        private enum ShaderPasses
        {
            CloneAlphamap = 0,
            CloneHeightmap
        }

        public enum MovementBehavior
        {
            Snap = 0,       // clone snaps back to set sample location on mouse up
            FollowOnPaint,  // clone location will move with the brush only when painting
            FollowAlways,   // clone location will move with the brush always
            Fixed,          // clone wont move at all and will sample same location always
        }

        [System.Serializable]
        struct BrushLocationData
        {
            public Terrain terrain;
            public Vector3 pos;

            public void Set(Terrain terrain, Vector3 pos)
            {
                this.terrain = terrain;
                this.pos = pos;
            }
        }

        [SerializeField] private MovementBehavior m_MovementBehavior;
        [SerializeField] private bool m_PaintHeightmap = true;
        [SerializeField] private bool m_PaintAlphamap = true;
        [SerializeField] private float m_StampingOffsetFromClone = 0.0f;

        // variables for keeping track of mouse and key presses and painting states
        private bool m_lmb;
        private bool m_ctrl;
        private bool m_wasPainting;
        private bool m_isPainting;
        private bool m_HasDoneFirstPaint;

        // The current brush location data we are sampling/cloning from
        private BrushLocationData m_SampleLocation;
        // Brush location defined when user ctrl-clicks. Where the sample location should
        // "snap" back to when the user is not painting and clone behavior == Snap
        [SerializeField] private BrushLocationData m_SnapbackLocation;
        // brush location data used for determining how much the user brush moved in a frame
        private BrushLocationData m_PrevBrushLocation;

        private static Material m_Material = null;
        private static Material GetPaintMaterial()
        {
            if (m_Material == null)
            {
                m_Material = new Material(Shader.Find("TerrainToolSamples/CloneBrush"));
            }

            return m_Material;
        }

        public override string GetName()
        {
            return "Utility/Clone Brush";
        }

        public override string GetDesc()
        {
            return Styles.descriptionString;
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();

            // draw button-like toggles for choosing which terrain textures to sample
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(Styles.cloneSourceContent);

                if (GUILayout.Button(Styles.cloneTextureContent, Styles.GetButtonToggleStyle(m_PaintAlphamap)))
                    m_PaintAlphamap = !m_PaintAlphamap;

                if (GUILayout.Button(Styles.cloneHeightmapContent, Styles.GetButtonToggleStyle(m_PaintHeightmap)))
                    m_PaintHeightmap = !m_PaintHeightmap;
            }
            EditorGUILayout.EndHorizontal();

            m_MovementBehavior = (MovementBehavior)EditorGUILayout.EnumPopup(Styles.cloneBehaviorContent, m_MovementBehavior);
            m_StampingOffsetFromClone = EditorGUILayout.Slider(Styles.heightOffsetContent, m_StampingOffsetFromClone,
                                                              -terrain.terrainData.size.y, terrain.terrainData.size.y);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
            {
                m_isPainting = false;
                m_HasDoneFirstPaint = false;
                Save(true);
            }
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            ProcessInput(terrain, editContext);
            UpdateBrushLocations(terrain, editContext);
            DrawBrushPreviews(terrain, editContext);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if (!m_isPainting || m_SampleLocation.terrain == null)
                return true;

            // grab brush transforms for the sample location (where we are cloning from)
            // and target location (where we are cloning to)
            Vector2 sampleUV = TerrainUVFromBrushLocation(m_SampleLocation.terrain, m_SampleLocation.pos);
            BrushTransform sampleBrushXform = TerrainPaintUtility.CalculateBrushTransform(m_SampleLocation.terrain, sampleUV, editContext.brushSize, 1);
            BrushTransform targetBrushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 1);

            // set material props that will be used for both heightmap and alphamap painting
            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, m_StampingOffsetFromClone * 0.5f, terrain.terrainData.size.y, 0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            // apply texture modifications to terrain
            if (m_PaintAlphamap) PaintAlphamap(m_SampleLocation.terrain, terrain, sampleBrushXform, targetBrushXform, mat);
            if (m_PaintHeightmap) PaintHeightmap(m_SampleLocation.terrain, terrain, sampleBrushXform, targetBrushXform, editContext, mat);

            return false;
        }

        private void ProcessInput(Terrain terrain, IOnSceneGUI editContext)
        {
            // update Left Mouse Button state
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && editContext.hitValidTerrain)
                m_lmb = true;
            else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                m_lmb = false;
            
            if(!m_isPainting)
            {
                if (Event.current.type == EventType.KeyDown &&
                    (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl))
                    m_ctrl = true;
                else if (Event.current.type == EventType.KeyUp &&
                        (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl))
                    m_ctrl = false;
            }
            
            m_wasPainting = m_isPainting;
            m_isPainting = m_lmb && !m_ctrl;
        }

        private void UpdateBrushLocations(Terrain terrain, IOnSceneGUI editContext)
        {
            if (!editContext.hitValidTerrain)
            {
                return;
            }

            if (!m_isPainting)
            {
                // check to see if the user is selecting a new location for the clone sample
                // and set the current sample location to that as well as the snap back location
                if(m_lmb && m_ctrl)
                {
                    m_HasDoneFirstPaint = false;
                    m_SampleLocation.Set(terrain, editContext.raycastHit.point);
                    m_SnapbackLocation.Set(terrain, editContext.raycastHit.point);
                }
                
                // snap the sample location back to the user-picked sample position
                if (m_MovementBehavior == MovementBehavior.Snap)
                {
                    m_SampleLocation.Set(m_SnapbackLocation.terrain, m_SnapbackLocation.pos);
                }
            }
            else if (!m_wasPainting && m_isPainting) // first frame of user painting
            {
                m_HasDoneFirstPaint = true;
                // check if the user just started painting. do this so a delta pos
                // isn't applied to the sample location on the first paint operation
                m_PrevBrushLocation.Set(terrain, editContext.raycastHit.point);
            }

            bool updateClone = (m_isPainting && m_MovementBehavior != MovementBehavior.Fixed) ||
                                (m_isPainting && m_MovementBehavior == MovementBehavior.FollowOnPaint) ||
                                (m_HasDoneFirstPaint && m_MovementBehavior == MovementBehavior.FollowAlways);
            
            if (updateClone)
            {
                HandleBrushCrossingSeams(ref m_SampleLocation, editContext.raycastHit.point, m_PrevBrushLocation.pos);
            }

            // update the previous paint location for use in the next frame (if the user is painting)
            m_PrevBrushLocation.Set(terrain, editContext.raycastHit.point);
        }

        // check to see if the sample brush is crossing any terrain seams/borders. have to do this manually
        // since TerrainPaintUtility only immediate neighbors and not manually created PaintContexts
        private void HandleBrushCrossingSeams(ref BrushLocationData brushLocation, Vector3 currBrushPos, Vector3 prevBrushPos)
        {
            if (brushLocation.terrain == null)
                return;

            Vector3 deltaPos = currBrushPos - prevBrushPos;
            brushLocation.Set(brushLocation.terrain, brushLocation.pos + deltaPos);

            Vector2 currUV = TerrainUVFromBrushLocation(brushLocation.terrain, brushLocation.pos);

            if (currUV.x >= 1.0f && brushLocation.terrain.rightNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.rightNeighbor;
            else if (currUV.x < 0.0f && brushLocation.terrain.leftNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.leftNeighbor;

            if (currUV.y >= 1.0f && brushLocation.terrain.topNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.topNeighbor;
            else if (currUV.y < 0.0f && brushLocation.terrain.bottomNeighbor != null)
                brushLocation.terrain = brushLocation.terrain.bottomNeighbor;
        }

        private void DrawBrushPreviews(Terrain terrain, IOnSceneGUI editContext)
        {
            Vector2 sampleUV;
            BrushTransform sampleXform;
            PaintContext sampleContext = null;
            Material previewMat = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();
            // draw sample location brush and create context data to be used when drawing target brush previews
            if (m_SampleLocation.terrain != null)
            {
                sampleUV = TerrainUVFromBrushLocation(m_SampleLocation.terrain, m_SampleLocation.pos);
                sampleXform = TerrainPaintUtility.CalculateBrushTransform(m_SampleLocation.terrain, sampleUV, editContext.brushSize, 0);
                sampleContext = TerrainPaintUtility.BeginPaintHeightmap(m_SampleLocation.terrain, sampleXform.GetBrushXYBounds());
                TerrainPaintUtilityEditor.DrawBrushPreview(sampleContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture,
                                                           editContext.brushTexture, sampleXform, previewMat, 0);
            }

            // draw brush preview and mesh preview for current mouse position
            if (editContext.hitValidTerrain)
            {
                BrushTransform targetXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0f);
                PaintContext targetContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, targetXform.GetBrushXYBounds(), 1);

                // draw basic preview of brush
                TerrainPaintUtilityEditor.DrawBrushPreview(targetContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture,
                                                           editContext.brushTexture, targetXform, previewMat, 0);

                if (sampleContext != null && m_PaintHeightmap)
                {
                    ApplyHeightmap(sampleContext, targetContext, targetXform, terrain, editContext.brushTexture, editContext.brushStrength);

                    // draw preview of brush mesh
                    RenderTexture.active = targetContext.oldRenderTexture;
                    previewMat.SetTexture("_HeightmapOrig", targetContext.sourceRenderTexture);
                    TerrainPaintUtilityEditor.DrawBrushPreview(targetContext, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture,
                                                               editContext.brushTexture, targetXform, previewMat, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(targetContext);
            }

            if (sampleContext != null)
            {
                TerrainPaintUtility.ReleaseContextResources(sampleContext);
            }
        }

        private Vector2 TerrainUVFromBrushLocation(Terrain terrain, Vector3 posWS)
        {
            // position relative to Terrain-space. doesnt handle rotations,
            // since that's not really supported at the moment
            Vector3 posTS = posWS - terrain.transform.position; 
            Vector3 size = terrain.terrainData.size;

            return new Vector2(posTS.x / size.x, posTS.z / size.z);
        }

        private void ApplyHeightmap(PaintContext sampleContext, PaintContext targetContext, BrushTransform targetXform,
                                    Terrain targetTerrain, Texture brushTexture, float brushStrength)
        {
            Material paintMat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, m_StampingOffsetFromClone * 0.5f, targetTerrain.terrainData.size.y, 0f);
            paintMat.SetTexture("_BrushTex", brushTexture);
            paintMat.SetVector("_BrushParams", brushParams);
            paintMat.SetTexture("_CloneTex", sampleContext.sourceRenderTexture);
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(targetContext, targetXform, paintMat);
            Graphics.Blit(targetContext.sourceRenderTexture, targetContext.destinationRenderTexture, paintMat, (int)ShaderPasses.CloneHeightmap);
        }

        private void PaintHeightmap(Terrain sampleTerrain, Terrain targetTerrain, BrushTransform sampleXform,
                                    BrushTransform targetXform, IOnPaint editContext, Material mat)
        {
            PaintContext sampleContext = TerrainPaintUtility.BeginPaintHeightmap(sampleTerrain, sampleXform.GetBrushXYBounds());
            PaintContext targetContext = TerrainPaintUtility.BeginPaintHeightmap(targetTerrain, targetXform.GetBrushXYBounds());
            ApplyHeightmap(sampleContext, targetContext, targetXform, targetTerrain, editContext.brushTexture, editContext.brushStrength);
            TerrainPaintUtility.EndPaintHeightmap(targetContext, "Terrain Paint - Clone Brush (Heightmap)");
            TerrainPaintUtility.ReleaseContextResources(sampleContext);
        }

        private void PaintAlphamap(Terrain sampleTerrain, Terrain targetTerrain, BrushTransform sampleXform, BrushTransform targetXform, Material mat)
        {
            Rect sampleRect = sampleXform.GetBrushXYBounds();
            Rect targetRect = targetXform.GetBrushXYBounds();
            int numSampleTerrainLayers = sampleTerrain.terrainData.terrainLayers.Length;

            for (int i = 0; i < numSampleTerrainLayers; ++i)
            {
                TerrainLayer layer = sampleTerrain.terrainData.terrainLayers[i];

                if (layer == null) continue; // nothing to paint if the layer is NULL

                PaintContext sampleContext = TerrainPaintUtility.BeginPaintTexture(sampleTerrain, sampleRect, layer);

                // manually create target context since we are possibly applying another terrain's layers and not its own
                int layerIndex = TerrainPaintUtility.FindTerrainLayerIndex(sampleTerrain, layer);
                Texture2D layerTexture = TerrainPaintUtility.GetTerrainAlphaMapChecked(sampleTerrain, layerIndex >> 2);
                PaintContext targetContext = PaintContext.CreateFromBounds(targetTerrain, targetRect, layerTexture.width, layerTexture.height);
                targetContext.CreateRenderTargets(RenderTextureFormat.R8);
                targetContext.GatherAlphamap(layer, true);
                sampleContext.sourceRenderTexture.filterMode = FilterMode.Point;
                mat.SetTexture("_CloneTex", sampleContext.sourceRenderTexture);
                Graphics.Blit(targetContext.sourceRenderTexture, targetContext.destinationRenderTexture, mat, (int)ShaderPasses.CloneAlphamap);
                // apply texture modifications and perform cleanup. same thing as calling TerrainPaintUtility.EndPaintTexture
                targetContext.ScatterAlphamap("Terrain Paint - Clone Brush (Texture)");
                targetContext.Cleanup();
            }
        }

        private static class Styles
        {
            public static readonly string descriptionString =
                                            "Clones terrain from another area of the terrain map to the selected location.\n\n" +
                                            "Hold Control and Left Click to assign the clone sample area.\n\n" +
                                            "Left Click to apply the cloned stamp.";
            public static readonly GUIContent cloneSourceContent = new GUIContent("Terrain sources to clone:",
                                            "Textures:\nBrush will gather and clone TerrainLayer data at Sample location\n\n" + 
                                            "Heightmap:\nBrush will gather and clone Heightmap data at Sample location");
            public static readonly GUIContent cloneTextureContent = new GUIContent("Textures", "Brush will gather and clone TerrainLayer data from Sample location");
            public static readonly GUIContent cloneHeightmapContent = new GUIContent("Heightmap", "Brush will gather and clone Heightmap data from Sample location");
            public static readonly GUIContent cloneBehaviorContent = new GUIContent("Clone Movement Behavior",
                                            "Snap:\nClone location will snap back to user-selected location on mouse-up\n\n" +
                                            "Follow On Paint:\nClone location will move with mouse position (only when painting) and not snap back\n\n" +
                                            "Follow Always:\nClone location will always move with mouse position (even when not painting) and not snap back\n\n" +
                                            "Fixed:\nClone location will always stay at the user-selected location");
            public static readonly GUIContent heightOffsetContent = new GUIContent("Height Offset", 
                                            "When stamping the heightmap, the cloned height will be added with this offset to raise or lower the cloned height at the stamp location.");

            public static GUIStyle buttonActiveStyle = null;
            public static GUIStyle buttonNormalStyle = null;

            static Styles()
            {
                buttonNormalStyle = "Button";
                buttonActiveStyle = new GUIStyle("Button");
                buttonActiveStyle.normal.background = buttonNormalStyle.active.background;
            }

            public static GUIStyle GetButtonToggleStyle(bool isToggled)
            {
                return isToggled ? buttonActiveStyle : buttonNormalStyle;
            }
        }
    }
}
