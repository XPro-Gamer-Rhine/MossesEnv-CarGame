using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace ProceduralWorlds.SceneOptimizer
{
    [InitializeOnLoad]
    public static class EditorEvents
    {
        #region Variables
        public static Action<string> onImportPackageCompleted;
        public static Action<string> onImportPackageCancelled;
        public static Action<string, string> onImportPackageFailed;
        public static Action onHeierarchyChanged;
        public static Action onEditorUpdate;
        public static Action onBeforeAssemblyReloads;
        public static Action onAfterAssemblyReloads;
        #endregion
        #region Constructors
        static EditorEvents()
        {
            // On Import Package Completed
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            // On Import Package Cancelled
            AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            // On Import Package Failed
            AssetDatabase.importPackageFailed -= OnImportPackageFailed;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            // On Before Assembly Reloads
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReloads;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReloads;
            // On After Assembly Reloads
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReloads;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReloads;
            // On Editor Update
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            // On Hierarchy Changed
            EditorApplication.hierarchyChanged -= OnHeierarchyChanged;
            EditorApplication.hierarchyChanged += OnHeierarchyChanged;
            // Initialize();
            SubscribeEvents();
        }
        #endregion
        #region Methods
        private static void SubscribeEvents()
        {
            PWEvents.Destroy = EditorDestroy;
            PWEvents.DisplayProgress = EditorDisplayProgress;
            PWEvents.ClearProgress = EditorClearProgress;
            PWEvents.SaveMeshToDisk = EditorSaveMeshToDisk;
        }
        private static void OnImportPackageCompleted(string packageName) => onImportPackageCompleted?.Invoke(packageName);
        /// <summary>
        /// Called when a package import is Cancelled.
        /// </summary>
        private static void OnImportPackageCancelled(string packageName) => onImportPackageCancelled?.Invoke(packageName);
        /// <summary>
        /// Called when a package import fails.
        /// </summary>
        private static void OnImportPackageFailed(string packageName, string error) => onImportPackageFailed?.Invoke(packageName, error);
        /// <summary>
        /// Called Before Assembly Reloads
        /// </summary>
        private static void OnBeforeAssemblyReloads()
        {
            onBeforeAssemblyReloads?.Invoke();
            // GeNaFactory.Dispose();
        }
        /// <summary>
        /// Called After Assembly Reloads
        /// </summary> 
        private static void OnAfterAssemblyReloads() => onAfterAssemblyReloads?.Invoke();
        /// <summary>
        /// Called when Editor Updates
        /// </summary>
        private static void OnEditorUpdate() => onEditorUpdate?.Invoke();
        /// <summary>
        /// Event that is raised when an object or group of objects in the hierarchy changes.
        /// </summary>
        private static void OnHeierarchyChanged() => onHeierarchyChanged?.Invoke();
        private static void EditorDestroy(Object @object) => Object.DestroyImmediate(@object);
        private static void EditorDisplayProgress(string title, string info, float progress) => UnityEditor.EditorUtility.DisplayProgressBar(title, info, progress);
        private static void EditorClearProgress() => UnityEditor.EditorUtility.ClearProgressBar();
        private static Mesh EditorSaveMeshToDisk(Scene scene, Mesh sharedMesh)
        {
            string scenePath = Path.GetDirectoryName(scene.path);
            string meshPath = $"{scenePath}\\{scene.name}_OptimizedObjects";
            string extension = ".asset";
            string filePath = $"{meshPath}\\{sharedMesh.name}{extension}";
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            AssetDatabase.CreateAsset(sharedMesh, filePath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(filePath);
        }
        #endregion
    }
}