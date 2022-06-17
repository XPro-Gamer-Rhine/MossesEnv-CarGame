using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ProceduralWorlds.SceneOptimizer
{
    public class RootObjectList : ReorderableListEditor<GameObjectEntry>
    {
        private List<GameObjectEntry> duplicates = new List<GameObjectEntry>();
        public bool HasDuplicates()
        {
            ScanForDuplicates();
            return duplicates.Count > 0;
        }
        private void ScanForDuplicates()
        {
            duplicates.Clear();
            List<string> names = new List<string>();
            Dictionary<GameObject, GameObjectEntry> uniqueEntries = new Dictionary<GameObject, GameObjectEntry>();
            foreach (GameObjectEntry entry in m_reorderableList.list)
            {
                GameObject gameObject = entry.GameObject;
                if (gameObject == null)
                    continue;
                if (uniqueEntries.ContainsKey(gameObject))
                {
                    // Duplicate found!
                    var a = uniqueEntries[gameObject];
                    if (!duplicates.Contains(a))
                        duplicates.Add(a);
                    if (!duplicates.Contains(entry))
                        duplicates.Add(entry);
                }
                else
                {
                    uniqueEntries.Add(gameObject, entry);
                }
            }
        }
        public bool HasNullRoots()
        {
            foreach (GameObjectEntry entry in m_reorderableList.list)
            {
                if (entry.GameObject == null)
                    return true;
            }
            return false;
        }
        protected override void DrawListHeader(Rect rect)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, ("Root GameObjects"));
            EditorGUI.indentLevel = oldIndent;
            ScanForDuplicates();
        }
        protected override void DrawListElement(Rect rect, GameObjectEntry entry, bool isFocused)
        {
            // Spawner Object
            EditorGUI.BeginChangeCheck();
            {
                var hasDuplicates = duplicates.Contains(entry);
                var isNull = entry.GameObject == null;
                var oldColor = GUI.color;
                if (isNull)
                    GUI.color = SceneOptimizerEditor.WARNING_COLOR;
                if (hasDuplicates)
                    GUI.color = SceneOptimizerEditor.ERROR_COLOR;
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + 1f, rect.width * 0.18f, EditorGUIUtility.singleLineHeight), "Active");
                entry.Enabled = EditorGUI.Toggle(new Rect(rect.x + rect.width * 0.18f, rect.y, rect.width * 0.1f, EditorGUIUtility.singleLineHeight), entry.Enabled);
                entry.GameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.x + rect.width * 0.4f, rect.y + 1f, rect.width * 0.6f, EditorGUIUtility.singleLineHeight), entry.GameObject, typeof(GameObject), true);
                EditorGUI.indentLevel = oldIndent;
                GUI.color = oldColor;
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }
    }
}