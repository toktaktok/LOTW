using UnityEngine;
using UnityEditor;
using Project.Scripts.Content.World;

namespace Project.Scripts.Editor
{
    [CustomEditor(typeof(RailNode))]
    public class RailNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RailNode currentNode = (RailNode)target;

            GUILayout.Space(20);
        
            GUI.backgroundColor = Color.green;
            if(GUILayout.Button("Create Next Node & Connect", GUILayout.Height(40)))
            {
                CreateNextNode(currentNode);
            }
            GUI.backgroundColor = Color.white;
        }

        private void CreateNextNode(RailNode current)
        {
            GameObject go = new GameObject($"RailNode_{current.transform.childCount}");
        
            go.transform.position = current.transform.position + Vector3.right * 2.0f;
            go.transform.parent = current.transform.parent;

            RailNode newNode = go.AddComponent<RailNode>();
        
            current.ConnectTo(newNode);

            Selection.activeGameObject = go;
        
            Undo.RegisterCreatedObjectUndo(go, "Create Connected Rail Node");
            EditorUtility.SetDirty(current);
        }
    }
}