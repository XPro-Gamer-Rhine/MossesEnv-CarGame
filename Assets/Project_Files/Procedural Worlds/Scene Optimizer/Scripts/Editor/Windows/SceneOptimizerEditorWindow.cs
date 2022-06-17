using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace ProceduralWorlds.SceneOptimizer
{
    public class SceneOptimizerEditorWindow : EditorWindow
    {
        public const string WINDOW_TITLE = "Scene Optimizer"; 
        #region Variables
        protected bool m_inited = false;
        public SceneOptimizer m_tools;
        public SceneOptimizerEditor m_editor;
        #endregion
        #region Properties
        public SceneOptimizer Tools
        {
            get
            {
                if (m_tools == null)
                    m_tools = Resources.Load<SceneOptimizer>(WINDOW_TITLE);
                return m_tools;
            }
        }
        public SceneOptimizerEditor ToolsEditor
        {
            get
            {
                if (m_editor == null)
                {
                    m_editor = Editor.CreateEditor(Tools) as SceneOptimizerEditor;
                }
                return m_editor;
            }
        }
        #endregion
        [MenuItem("Window/Procedural Worlds/Scene Optimizer/Main Window...", priority = 41)]
        public static void Open()
        {
            SceneOptimizerEditorWindow win = GetWindow<SceneOptimizerEditorWindow>();
            win.titleContent = new GUIContent(WINDOW_TITLE);
            win.minSize = new Vector2(300f, 300f);
            win.Show();
        }
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.hierarchyWindowItemOnGUI -= DetectInput;
        }
        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyWindowItemOnGUI -= DetectInput;
            EditorApplication.hierarchyWindowItemOnGUI += DetectInput;
            ToolsEditor.OnFocus();
        }
        private void OnLostFocus()
        {
            ToolsEditor.OnLostFocus();
        }
        private void DetectInput(int instanceID, Rect selectionRect)
        {
            if (instanceID == Selection.activeInstanceID)
            {
                ToolsEditor.OnSceneGUI();
            }
        }
        private void DrawHandles(OptimizeCommand command, List<GameObjectEntry> entries)
        {
            Vector3 cellSize = command.CellSize;
            Vector3 offset = command.CellOffset;
            Handles.color = command.VisualizationColor;
            SpatialHashing<Transform> hash = new SpatialHashing<Transform>(cellSize, offset);
            Dictionary<int, Transform> transforms = PWUtility.GetUniqueTransforms(command, entries);
            foreach (KeyValuePair<int, Transform> pair in transforms)
            {
                Transform transform = pair.Value;
                Vector3 position = transform.position;
                hash.Insert(position, transform);
            }
            foreach (KeyValuePair<Vector3Int, List<Transform>> pair in hash.chunks)
            {
                Vector3Int key = pair.Key;
                Vector3 center = Vector3.Scale(key, cellSize);
                center += offset;
                Handles.DrawWireCube(center, cellSize);
            }
            Bounds bounds = hash.GetBounds();
            Handles.color = Color.yellow;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }
        private void OnSceneGUI(SceneView sceneView)
        {
            ToolsEditor.OnSceneGUI();
            // Draw Handles
            OptimizeCommand selected = ToolsEditor.OptimizeCommandList.Selected;
            if (selected != null)
            {
                Tools sceneOptimization = ToolsEditor.SceneOptimization;
                List<GameObjectEntry> allRoots = sceneOptimization.GetAllRoots();
                Handles.color = Color.blue;
                DrawHandles(selected, allRoots);
                float objectLayerCullingDistance = selected.ObjectlayerCullingDistance;
                float shadowLayerCullingDistance = selected.ShadowLayerCullingDistance;
                Color objectColor = selected.ObjectVisualizationColor;
                Color shadowColor = selected.ShadowVisualizationColor;
                EditorUtility.RenderSceneCullingCamera(objectLayerCullingDistance, shadowLayerCullingDistance, objectColor, shadowColor);
                HandleUtility.Repaint();
            }
        }
        private void OnGUI()
        {
            ToolsEditor.OnInspectorGUI();
        }
    }
}