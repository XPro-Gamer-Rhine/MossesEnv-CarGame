using UnityEditor;
using UnityEngine;
namespace ProceduralWorlds.SceneOptimizer
{
    public class EditorExtensionsWindow : EditorWindow
    {
        public static string windowTitle = "Editor Extensions";
        protected bool m_controlSceneCamera;
        protected float m_rotationSpeedY = 0.0f;
        protected bool m_controlZoom = false;
        protected Transform m_startPoint;
        protected Vector3 m_endPoint;
        protected float m_zoomSpeed = 10.0f;
        protected Transform m_waypoint1, m_waypoint2;
        private float m_zoomTime = 0.0f;
        private bool m_zooming = false;
        private SceneView m_sceneView;
        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/Procedural Worlds/Scene Optimizer/Editor Extensions")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            EditorExtensionsWindow window = (EditorExtensionsWindow)EditorWindow.GetWindow(typeof(EditorExtensionsWindow), false, windowTitle);
            window.Show();
        }
        private void OnEnable()
        {
            m_sceneView = SceneView.lastActiveSceneView;
        }
        private void Update()
        {
            if (m_sceneView != null)
            {
                Vector3 euler = m_sceneView.rotation.eulerAngles;
                euler.y += m_rotationSpeedY * PWEditorTime.deltaTime;
                m_sceneView.rotation = Quaternion.Euler(euler);
                if (m_controlZoom)
                {
                    if (m_startPoint != null)
                    {
                        m_sceneView.pivot = Vector3.MoveTowards(m_startPoint.position, m_waypoint2.position, m_zoomTime);
                    }
                    if (m_waypoint1 != null && m_waypoint2 != null)
                    {
                        if (m_zooming)
                        {
                            m_zoomTime += m_zoomSpeed * PWEditorTime.deltaTime;
                            m_sceneView.pivot = Vector3.MoveTowards(m_waypoint1.position, m_waypoint2.position, m_zoomTime);
                            float distance = Vector3.Distance(m_sceneView.pivot, m_waypoint2.position);
                            if (distance <= 0.1f)
                            {
                                m_zooming = false;
                            }
                        }
                    }
                }
            }
        }
        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            m_controlSceneCamera = EditorGUILayout.BeginToggleGroup("Control Scene Camera", m_controlSceneCamera);
            {
                EditorGUI.indentLevel++;
                m_rotationSpeedY = EditorGUILayout.FloatField("Rotation Speed (Y)", m_rotationSpeedY);
                m_controlZoom = EditorGUILayout.BeginToggleGroup("Control Zoom", m_controlZoom);
                {
                    EditorGUI.indentLevel++;
                    m_zoomSpeed = EditorGUILayout.FloatField("Zoom Speed", m_zoomSpeed);
                    m_waypoint1 = (Transform)EditorGUILayout.ObjectField("Waypoint 1", m_waypoint1, typeof(Transform), true);
                    m_waypoint2 = (Transform)EditorGUILayout.ObjectField("Waypoint 2", m_waypoint2, typeof(Transform), true);
                    if (m_waypoint1 != null && m_waypoint2 != null)
                    {
                        if (GUILayout.Button("Zoom"))
                        {
                            m_zooming = true;
                            m_zoomTime = 0.0f;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndToggleGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndToggleGroup();
        }
    }
}