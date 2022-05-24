using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace ProceduralWorlds.SceneOptimizer
{
    [CustomEditor(typeof(SceneOptimizer))]
    public class SceneOptimizerEditor : SceneOptimizerBaseEditor
    {
        public const string OBJECT_LAYER_CULLING = "Object Layer Culling";
        public const string SHADOW_LAYER_CULLING = "Shadow Layer Culling";
        public const string FPS_TESTER_ROOT_NAME = "Scene Optimizer - FPS Tester";
        public static readonly Color WARNING_COLOR = new Color(1f, 0.8f, 0.34f);
        public static readonly Color ERROR_COLOR = new Color(1f, 0.52f, 0.41f);
        private static Color ACTION_BUTTON_COLOR = new Color(0.4666667f, 0.6666667f, 0.2352941f);
        private static Color ACTION_BUTTON_PRO_COLOR = new Color(0.2117647f, 0.3176471f, 0.09019608f);
        public static Color ActionButtonColor => EditorGUIUtility.isProSkin ? ACTION_BUTTON_PRO_COLOR : ACTION_BUTTON_COLOR;
        private static bool m_generalSettingsPanel = false;
        private static bool m_keyBindingsPanel = false;
        private static bool m_sceneOptimizationPanel = true;
        private static int m_selectedOptimizeCommand = 0;
        private SceneOptimizer m_tools;
        private RootObjectList m_rootObjectList = new RootObjectList();
        private OptimizeCommandList m_optimizeCommandList = new OptimizeCommandList();
        private MaterialList m_materialEntries = new MaterialList();
        private List<MaterialEntry> m_filteredEntries = new List<MaterialEntry>();
        private Vector2 scrollPos;
        private bool m_infoPresent = false;
        private bool m_warningsPresent = false;
        private bool m_errorsPresent = false;
        public OptimizeCommandList OptimizeCommandList => m_optimizeCommandList;
        public Tools SceneOptimization => m_tools.SceneOptimizer;
        public List<OptimizeCommand> OptimizeCommands => SceneOptimization.OptimizeCommands;
        public OptimizeCommand OptimizeCommand
        {
            get
            {
                OptimizeCommand result = null;
                if (m_selectedOptimizeCommand <= 0 || m_selectedOptimizeCommand >= OptimizeCommands.Count)
                    result = OptimizeCommands[m_selectedOptimizeCommand];
                return result;
            }
        }
        private void OnEnable()
        {
            if (m_editorUtils == null)
                m_editorUtils = PWApp.GetEditorUtils(this);
            m_tools = target as SceneOptimizer;
            m_rootObjectList.Create(SceneOptimization.RootGameObjects);
            m_rootObjectList.OnChanged = MarkDirty;
            m_optimizeCommandList.Create(OptimizeCommands);
            m_optimizeCommandList.OnChanged = MarkDirty;
            m_materialEntries.Create(m_filteredEntries, false, true, true, true, true);
            m_materialEntries.OnChanged = MarkDirty;
            m_materialEntries.SceneOptimization = SceneOptimization;
            m_optimizeCommandList.OnSelectionChangedEvent -= OnSelectionChanged;
            m_optimizeCommandList.OnSelectionChangedEvent += OnSelectionChanged;
        }
        private void MarkDirty()
        {
            UnityEditor.EditorUtility.SetDirty(m_tools);
        }
        private void OnSelectionChanged(int index)
        {
            OptimizeCommand command = m_optimizeCommandList.Selected;
            if (command == null)
                return;
            m_materialEntries.CurrentCommand = command;
        }
        /// <summary>
        /// Handle drop area for new objects
        /// </summary>
        public bool DrawRootGameObjectGUI()
        {
            // Ok - set up for drag and drop
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop GameObjects", Styles.dropPanel);
            if (evt.type == EventType.DragPerform || evt.type == EventType.DragUpdated)
            {
                if (!dropArea.Contains(evt.mousePosition))
                    return false;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    //Handle game objects / prefabs
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go)
                        {
                            SceneOptimization.RootGameObjects.Add(new GameObjectEntry
                            {
                                Enabled = true,
                                GameObject = go
                            });
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            m_editorUtils.GUIHeader();
            m_editorUtils.GUINewsHeader();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, Styles.panel);
            {
                EditorGUI.BeginChangeCheck();
                m_generalSettingsPanel = m_editorUtils.Panel("Settings", SettingsPanel, m_generalSettingsPanel);
                m_keyBindingsPanel = m_editorUtils.Panel("KeyBindings", KeyBindingsPanel, m_keyBindingsPanel);
                m_sceneOptimizationPanel = m_editorUtils.Panel("SceneOptimization", SceneOptimizationPanel, m_sceneOptimizationPanel);
                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditor.EditorUtility.SetDirty(m_tools);
                }
            }
            EditorGUILayout.EndScrollView();
            m_editorUtils.GUINewsFooter();
        }
        public void PerformSceneOptimization()
        {
            Undo.SetCurrentGroupName("Scene Optimization");
            int group = Undo.GetCurrentGroup();
            List<GameObjectEntry> originalRoots = SceneOptimization.GetAllRoots();
            foreach (GameObjectEntry entry in originalRoots)
            {
                if (entry == null)
                    continue;
                GameObject gameObject = entry.GameObject;
                if (gameObject == null)
                    continue;
                Undo.RegisterFullObjectHierarchyUndo(gameObject, "transform selected objects");
            }
            OptimizeCall optimizeCall = new OptimizeCall();
            List<GameObject> optimizedRoots = m_tools.ProcessSceneOptimization(optimizeCall, originalRoots);
            if (optimizedRoots.Count > 0)
            {
                if (SceneOptimization.SaveToDisk)
                {
                    AssetDatabase.StartAssetEditing();
                    Dictionary<Scene, string> sceneFullPaths = new Dictionary<Scene, string>();
                    List<Scene> uniqueScenes = GetUniqueScenes(optimizedRoots);
                    foreach (Scene scene in uniqueScenes)
                    {
                        string scenePath = Path.GetDirectoryName(scene.path);
                        string folderName = $"{scene.name}_OptimizedObjects";
                        string fullPath = $"{scenePath}\\{folderName}";
                        if (Directory.Exists(fullPath))
                        {
                            int count = EditorUtility.CountDirectories(scenePath, folderName);
                            folderName += $" {count}";
                        }
                        fullPath = $"{scenePath}\\{folderName}";
                        sceneFullPaths.Add(scene, fullPath);
                    }
                    foreach (GameObject rootObject in optimizedRoots)
                    {
                        Scene scene = rootObject.scene;
                        if (!sceneFullPaths.ContainsKey(scene))
                            continue;
                        string fullPath = sceneFullPaths[scene];
                        MeshFilter[] meshFilters = rootObject.GetComponentsInChildren<MeshFilter>();
                        MeshCollider[] meshColliders = rootObject.GetComponentsInChildren<MeshCollider>();
                        foreach (MeshFilter meshFilter in meshFilters)
                            meshFilter.sharedMesh = EditorUtility.SaveMeshToDisk(fullPath, meshFilter.sharedMesh);
                        foreach (MeshCollider meshCollider in meshColliders)
                            meshCollider.sharedMesh = EditorUtility.SaveMeshToDisk(fullPath, meshCollider.sharedMesh);
                    }
                    AssetDatabase.StopAssetEditing();
                }
                Tools sceneOptimization = m_tools.SceneOptimizer;
                if (sceneOptimization.DebugPerformance)
                {
                    GameObject fpsTester = sceneOptimization.FpsTesterPrefab;
                    if (fpsTester != null)
                    {
                        Original[] existingPWOriginals = FindObjectsOfType<Original>();
                        foreach (Original existing in existingPWOriginals)
                        {
                            PWEvents.Destroy(existing);
                        }
                        foreach (GameObjectEntry original in originalRoots)
                        {
                            GameObject gameObject = original.GameObject;
                            if (gameObject == null)
                                continue;
                            Original originalScript = gameObject.GetComponent<Original>();
                            if (originalScript != null)
                                continue;
                            gameObject.AddComponent<Original>();
                        }
                        GameObject fpsTest = GameObject.Find(FPS_TESTER_ROOT_NAME);
                        if (fpsTest != null)
                        {
                            PWEvents.Destroy(fpsTest);
                        }
                        fpsTest = new GameObject(FPS_TESTER_ROOT_NAME);
                        Transform fpsTestTransform = fpsTest.transform;
                        Optimized[] existingPWOptimizeds = FindObjectsOfType<Optimized>();
                        foreach (Optimized existing in existingPWOptimizeds)
                        {
                            PWEvents.Destroy(existing);
                        }
                        foreach (GameObject gameObject in optimizedRoots)
                        {
                            Optimized optimizedScript = gameObject.GetComponent<Optimized>();
                            if (optimizedScript != null)
                                continue;
                            gameObject.AddComponent<Optimized>();
                            gameObject.transform.SetParent(fpsTestTransform);
                        }
                        // Check if an instance already exists
                        FPSTester fpsTesterInstance = FindObjectOfType<FPSTester>();
                        if (fpsTesterInstance == null)
                        {
                            // Create Instance of FPS Tester
                            GameObject instance = PrefabUtility.InstantiatePrefab(fpsTester) as GameObject;
                            instance.transform.SetParent(fpsTestTransform);
                        }
                    }
                    else
                    {
                        PWDebug.LogWarning("FPS Tester is Missing!");
                    }
                }
                Selection.objects = optimizedRoots.ToArray();
                AssetDatabase.SaveAssets();
            }
            Undo.CollapseUndoOperations(group);
        }
        public List<Scene> GetUniqueScenes(List<GameObject> gameObjects)
        {
            List<Scene> scenes = new List<Scene>();
            foreach (GameObject gameObject in gameObjects)
            {
                Scene scene = gameObject.scene;
                if (scenes.Contains(gameObject.scene))
                    continue;
                scenes.Add(scene);
            }
            return scenes;
        }
        public override void OnSceneGUI()
        {
            base.OnSceneGUI();
            if (m_tools == null)
                return;
            Event e = Event.current;
            if (e == null)
                return;
            if (e.control)
            {
                Settings settings = m_tools.Settings;
                KeyBindings keyBindings = m_tools.KeyBindings;
                Tools sceneOptimization = m_tools.SceneOptimizer;
                bool process = false;
                bool raiseOrLower = false;
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == keyBindings.SnapToGroundKey)
                    {
                        settings.SnapToGround = true;
                        settings.AlignToGround = false;
                        settings.MoveUp = false;
                        settings.MoveDown = false;
                        process = true;
                    }
                    else if (e.keyCode == keyBindings.AlignToGroundKey)
                    {
                        settings.SnapToGround = false;
                        settings.AlignToGround = true;
                        settings.MoveUp = false;
                        settings.MoveDown = false;
                        process = true;
                    }
                    else if (e.keyCode == keyBindings.AlignAndSnapToGroundKey)
                    {
                        settings.SnapToGround = true;
                        settings.AlignToGround = true;
                        settings.MoveUp = false;
                        settings.MoveDown = false;
                        process = true;
                    }
                    else if (e.keyCode == keyBindings.RaiseFromGroundKey || e.keyCode == KeyCode.KeypadPlus)
                    {
                        settings.MoveUp = true;
                        settings.MoveDown = false;
                        raiseOrLower = true;
                    }
                    else if (e.keyCode == keyBindings.LowerInGroundKey || e.keyCode == KeyCode.KeypadMinus)
                    {
                        settings.MoveDown = true;
                        settings.MoveUp = false;
                        raiseOrLower = true;
                    }
                    else if (e.keyCode == keyBindings.OptimizeKey)
                    {
                        PerformSceneOptimization();
                        e.Use();
                    }
                    if (process)
                    {
                        Undo.SetCurrentGroupName("Processed Selection");
                        int group = Undo.GetCurrentGroup();
                        foreach (GameObject gameObject in Selection.gameObjects)
                            Undo.RegisterFullObjectHierarchyUndo(gameObject, "transform selected objects");
                        m_tools.ProcessSelectedObjects(Selection.gameObjects);
                        Undo.CollapseUndoOperations(group);
                        e.Use();
                    }
                    else if (raiseOrLower)
                    {
                        Undo.SetCurrentGroupName("Raised or Lowered");
                        int group = Undo.GetCurrentGroup();
                        foreach (GameObject gameObject in Selection.gameObjects)
                            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Raised or Lowered");
                        m_tools.RaiseOrLower(Selection.gameObjects);
                        Undo.CollapseUndoOperations(group);
                        e.Use();
                    }
                }
            }
        }
        private void SettingsPanel(bool helpEnabled)
        {
            Settings settings = m_tools.Settings;
            settings.SnapMode = (Constants.SnapMode)m_editorUtils.EnumPopup("SettingsSnapMode", settings.SnapMode, helpEnabled);
            settings.OffsetCheck = m_editorUtils.FloatField("SettingsOffsetCheck", settings.OffsetCheck, helpEnabled);
            settings.DistanceCheck = m_editorUtils.FloatField("SettingsDistanceCheck", settings.DistanceCheck, helpEnabled);
            settings.RaiseAndLowerAmount = m_editorUtils.FloatField("SettingsRaiseAndLowerAmount", settings.RaiseAndLowerAmount, helpEnabled);
        }
        private void KeyBindingsPanel(bool helpEnabled)
        {
            KeyBindings keyBindings = m_tools.KeyBindings;
            GUI.enabled = false;
            SceneOptimizer.m_firstKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsHoldDownKey", SceneOptimizer.m_firstKey, helpEnabled);
            GUI.enabled = true;
            keyBindings.SnapToGroundKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsSnapToGroundKey", keyBindings.SnapToGroundKey, helpEnabled);
            keyBindings.AlignToGroundKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsAlignToSlopeKey", keyBindings.AlignToGroundKey, helpEnabled);
            keyBindings.AlignAndSnapToGroundKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsSnapAndAlignToGroundKey", keyBindings.AlignAndSnapToGroundKey, helpEnabled);
            keyBindings.RaiseFromGroundKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsRaiseFromGroundKey", keyBindings.RaiseFromGroundKey, helpEnabled);
            keyBindings.LowerInGroundKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsLowerInGroundKey", keyBindings.LowerInGroundKey, helpEnabled);
            keyBindings.OptimizeKey = (KeyCode)m_editorUtils.EnumPopup("KeyBindingsOptimizeKey", keyBindings.OptimizeKey, helpEnabled);
        }
        private bool IsStandalonePlatform()
        {
            BuildTarget windows = BuildTarget.StandaloneWindows;
            BuildTarget windows64 = BuildTarget.StandaloneWindows64;
            BuildTarget mac = BuildTarget.StandaloneOSX;
            BuildTarget linux = BuildTarget.StandaloneLinux64;
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            return target == windows || target == windows64 || target == mac || target == linux;
        }
        private void OptimizeCommandPanel(OptimizeCommand command, bool helpEnabled)
        {
            #region General Settings
            m_editorUtils.Heading("Settings");
            m_editorUtils.InlineHelp("Settings", helpEnabled);
            EditorGUI.indentLevel++;
            {
                command.IsStatic = m_editorUtils.Toggle("IsStatic", command.IsStatic, helpEnabled);
                command.MeshFormat = (IndexFormat)m_editorUtils.EnumPopup("MeshFormat", command.MeshFormat, helpEnabled);
                bool performanceIssues = command.MeshFormat == IndexFormat.UInt32 && !IsStandalonePlatform();
                if (performanceIssues)
                {
                    EditorGUILayout.HelpBox("UInt32 can cause performance issues in the current platform. Consider switching to UInt16.", MessageType.Warning);
                    m_warningsPresent |= performanceIssues;
                }
                command.MeshLayer = m_editorUtils.LayerField("MeshLayer", command.MeshLayer, helpEnabled);
                command.AddLayerCulling = m_editorUtils.Toggle("AddLayerCulling", command.AddLayerCulling, helpEnabled);
                if (command.AddLayerCulling)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUI.indentLevel++;
                        {
                            GUI.SetNextControlName(OBJECT_LAYER_CULLING);
                            command.ObjectlayerCullingDistance = m_editorUtils.FloatField("ObjectDistance", command.ObjectlayerCullingDistance, helpEnabled);
                            GUI.SetNextControlName(SHADOW_LAYER_CULLING);
                            command.ShadowLayerCullingDistance = m_editorUtils.FloatField("ShadowDistance", command.ShadowLayerCullingDistance, helpEnabled);
                            command.ObjectVisualizationColor = m_editorUtils.ColorField("ObjectVizColor", command.ObjectVisualizationColor, helpEnabled);
                            command.ShadowVisualizationColor = m_editorUtils.ColorField("ShadowVizColor", command.ShadowVisualizationColor, helpEnabled);
                        }
                        EditorGUI.indentLevel--;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        CullingSystem cullingSystem = EditorUtility.GetCullingSystem();
                        if (cullingSystem != null)
                        {
                            cullingSystem.SetObjectCullingDistance(command.MeshLayer, command.ObjectlayerCullingDistance);
                            cullingSystem.SetShadowCullingDistance(command.MeshLayer, command.ShadowLayerCullingDistance);
                        }
                    }
                }
                command.DisableRenderers = m_editorUtils.Toggle("DisableRenderers", command.DisableRenderers, helpEnabled);
                bool backupNotification = command.DisableRenderers;
                if (backupNotification)
                {
                    EditorGUILayout.HelpBox("This setting will modify the original objects. You may want to make a backup of the original objects before combining.", MessageType.Info);
                    m_infoPresent |= backupNotification;
                }
                command.MergeColliders = m_editorUtils.Toggle("MergeColliders", command.MergeColliders, helpEnabled);
                command.AddColliders = m_editorUtils.Toggle("AddColliders", command.AddColliders, helpEnabled);
                if (command.AddColliders)
                {
                    EditorGUI.indentLevel++;
                    {
                        command.AddColliderLayer = m_editorUtils.LayerField("AddColliderLayer", command.AddColliderLayer);
                    }
                    EditorGUI.indentLevel--;
                }
                command.VisualizationColor = m_editorUtils.ColorField("VisualizationColor", command.VisualizationColor, helpEnabled);
                command.UseLargeRanges = m_editorUtils.Toggle("UseLargeRanges", command.UseLargeRanges, helpEnabled);
            }
            EditorGUI.indentLevel--;
            #endregion
            #region Lod Groups
            m_editorUtils.Heading("LodGroups");
            m_editorUtils.InlineHelp("LodGroups", helpEnabled);
            EditorGUI.indentLevel++;
            if (command.UseLargeRanges)
                command.LodSizeMultiplier = m_editorUtils.FloatField("LodSizeMultiplier", command.LodSizeMultiplier, helpEnabled);
            else
                command.LodSizeMultiplier = m_editorUtils.Slider("LodSizeMultiplier", command.LodSizeMultiplier, 0f, 1f, helpEnabled);
            command.AddLodGroup = m_editorUtils.Toggle("AddLodGroup", command.AddLodGroup, helpEnabled);
            if (command.AddLodGroup)
            {
                EditorGUI.indentLevel++;
                if (command.UseLargeRanges)
                    command.LodCullPercentage = m_editorUtils.FloatField("LodCullPercentage", command.LodCullPercentage, helpEnabled);
                else
                    command.LodCullPercentage = m_editorUtils.Slider("LodCullPercentage", command.LodCullPercentage, 99f, 1f, helpEnabled);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            #endregion
            #region Spatial Parition
            m_editorUtils.Heading("SpatialPartition");
            m_editorUtils.InlineHelp("SpatialPartition", helpEnabled);
            EditorGUI.indentLevel++;
            if (command.UseLargeRanges)
            {
                command.CellSize = m_editorUtils.Vector3Field("CellSize", command.CellSize, helpEnabled);
                command.CellOffset = m_editorUtils.Vector3Field("CellOffset", command.CellOffset, helpEnabled);
            }
            else
            {
                Vector3 cellSize = command.CellSize;
                Vector3 offset = command.CellOffset;
                EditorGUI.BeginChangeCheck();
                {
                    cellSize.x = cellSize.y = cellSize.z = m_editorUtils.FloatField("CellSize", cellSize.x, helpEnabled);
                    offset.x = offset.y = offset.z = m_editorUtils.FloatField("CellOffset", offset.x, helpEnabled);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    command.CellSize = cellSize;
                    command.CellOffset = offset;
                }
            }
            EditorGUI.indentLevel--;
            #endregion
            #region Filters
            m_editorUtils.Heading("Filters");
            m_editorUtils.InlineHelp("Filters", helpEnabled);
            EditorGUI.indentLevel++;
            if (command.UseLargeRanges)
            {
                command.MinObjectSize = m_editorUtils.FloatField("MinObjectSize", command.MinObjectSize, helpEnabled);
                command.MaxObjectSize = m_editorUtils.FloatField("MaxObjectSize", command.MaxObjectSize, helpEnabled);
            }
            else
            {
                float minObjectSize = command.MinObjectSize;
                float maxObjectSize = command.MaxObjectSize;
                m_editorUtils.SliderRange("ObjectSizeRange", ref minObjectSize, ref maxObjectSize, helpEnabled, 0, 1024);
                command.MinObjectSize = minObjectSize;
                command.MaxObjectSize = maxObjectSize;
            }
            command.FilterMaterials = m_editorUtils.Toggle("FilterMaterials", command.FilterMaterials, helpEnabled);
            if (command.FilterMaterials)
            {
                m_materialEntries.DrawList();
                m_editorUtils.InlineHelp("MaterialEntries", helpEnabled);
                if (m_editorUtils.Button("ClearAllMaterials", helpEnabled))
                {
                    command.ClearAllMaterials();
                }
            }
            EditorGUI.indentLevel--;
            #endregion
            EditorGUILayout.Space(6f);
        }
        private void SceneOptimizationPanel(bool helpEnabled)
        {
            m_infoPresent = false;
            m_warningsPresent = false;
            m_errorsPresent = false;
            Tools sceneOptimization = m_tools.SceneOptimizer;
            m_editorUtils.Heading("TitleOriginalGameObjects");
            m_editorUtils.InlineHelp("TitleOriginalGameObjects", helpEnabled);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawRootGameObjectGUI();
                m_rootObjectList.DrawList();
                bool hasDuplicates = m_rootObjectList.HasDuplicates();
                if (hasDuplicates)
                {
                    EditorGUILayout.HelpBox("You have root entries with the same GameObjects!", MessageType.Error);
                    m_errorsPresent |= hasDuplicates;
                }
                bool nullRoots = m_rootObjectList.HasNullRoots();
                if (nullRoots)
                {
                    EditorGUILayout.HelpBox("You have some null root objects!", MessageType.Warning);
                    m_warningsPresent |= nullRoots;
                }
                bool emptyRoots = m_rootObjectList.IsEmpty;
                if (emptyRoots)
                {
                    EditorGUILayout.HelpBox("You need to provide Root GameObjects that contain Meshes in order to optimize.", MessageType.Info);
                    m_infoPresent |= emptyRoots;
                }
                m_editorUtils.InlineHelp("RootGameObjects", helpEnabled);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            m_editorUtils.Heading("TitleOptimizationSettings");
            m_editorUtils.InlineHelp("TitleOptimizationSettings", helpEnabled);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                bool hasDuplicates = m_optimizeCommandList.HasDuplicates();
                m_optimizeCommandList.DrawList();
                if (hasDuplicates)
                {
                    EditorGUILayout.HelpBox("You cannot have Optimize Commands with the same names!", MessageType.Error);
                    m_errorsPresent |= hasDuplicates;
                }
                m_editorUtils.InlineHelp("OptimizeCommands", helpEnabled);
                OptimizeCommand selected = m_optimizeCommandList.Selected;
                if (selected != null)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        OptimizeCommandPanel(m_optimizeCommandList.Selected, helpEnabled);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditor.EditorUtility.SetDirty(m_tools);
                        SceneView.RepaintAll();
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.Space(3f);
                sceneOptimization.SaveToDisk = m_editorUtils.Toggle("SaveToDisk", sceneOptimization.SaveToDisk, helpEnabled);
                bool oldEnabled = GUI.enabled;
                if (sceneOptimization.DebugPerformance)
                    GUI.enabled = false;
                sceneOptimization.ChildUnderRoots = m_editorUtils.Toggle("ChildUnderRoots", sceneOptimization.ChildUnderRoots, helpEnabled);
                GUI.enabled = oldEnabled;
                sceneOptimization.DebugPerformance = m_editorUtils.Toggle("DebugPerformance", sceneOptimization.DebugPerformance, helpEnabled);
                EditorGUILayout.Space(3f);
                if (m_errorsPresent)
                {
                    EditorGUILayout.HelpBox("You need to fix all Errors above before Combining Meshes!", MessageType.Error);
                }
                if (m_warningsPresent)
                {
                    EditorGUILayout.HelpBox("Be sure to address any warnings before Combining Meshes.", MessageType.Warning);
                }
                EditorGUILayout.BeginHorizontal();
                {
                    if (m_editorUtils.Button("ResetToDefaults", GUILayout.Height(30f)))
                    {
                        if (UnityEditor.EditorUtility.DisplayDialog("Reset to Defaults", "Are you sure you want to reset all of the Scene Optimization settings?", "Yes", "No"))
                            sceneOptimization.ResetToDefaults();
                    }
                    oldEnabled = GUI.enabled;
                    if (m_errorsPresent)
                        GUI.enabled = false;
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = ActionButtonColor;
                    if (m_editorUtils.Button("ActionButton", GUILayout.Height(30f)))
                        PerformSceneOptimization();
                    GUI.backgroundColor = oldColor;
                    GUI.enabled = oldEnabled;
                }
                EditorGUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("ResetToDefaults", helpEnabled);
                m_editorUtils.InlineHelp("ActionButton", helpEnabled);
                EditorGUILayout.Space(3f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }
    }
}