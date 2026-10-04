using UnityEngine;
using UnityEditor;
using Project.Scripts.Content.World;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor
{
    [CustomEditor(typeof(RailNode))]
    public class RailNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _neighborsProp;
        private SerializedProperty _colorProp;
        private SerializedProperty _radiusProp;
        
        private void OnEnable()
        {
            _neighborsProp = serializedObject.FindProperty("neighbors");
            _colorProp = serializedObject.FindProperty("nodeColor");
            _radiusProp = serializedObject.FindProperty("radius");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool createNext = false;
            
            //Color, Radius
            EditorGUILayout.PropertyField(_colorProp);
            EditorGUILayout.PropertyField(_radiusProp);
            
            //Neighbor
            DrawNeighborProperty();

            if(_neighborsProp.arraySize < 2)
            {
                GUILayout.Space(20);
        
                GUI.backgroundColor = Color.goldenRod;
                if(GUILayout.Button("Create Next Node & Connect", GUILayout.Height(40)))
                {
                    createNext = true;
                }
            }
            GUI.backgroundColor = Color.white;
            serializedObject.ApplyModifiedProperties();

            //직접 수정하므로 ApplyModifiedProperties 이후에 실행
            if(createNext)
                CreateNextNode((RailNode)target);
        }
        private void DrawNeighborProperty()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Connections (Max 2)", EditorStyles.boldLabel);
            
            int nodeCount = _neighborsProp.arraySize;
            int removeIndex = -1;
            for(int index=0; index<nodeCount; ++index)
            {
                EditorGUILayout.BeginHorizontal();
                SerializedProperty element = _neighborsProp.GetArrayElementAtIndex(index);
                EditorGUILayout.PropertyField(element, new GUIContent($"Slot {index+1}"));

                if(GUILayout.Button("X", GUILayout.Width(20)))
                    removeIndex = index;
                EditorGUILayout.EndHorizontal();
            }

            if(removeIndex >= 0)
            {
                //non-null 오브젝트 참조는 첫 Delete에서 null 처리만 될 수 있음
                _neighborsProp.DeleteArrayElementAtIndex(removeIndex);
                if(_neighborsProp.arraySize == nodeCount)
                    _neighborsProp.DeleteArrayElementAtIndex(removeIndex);
            }
            
            if(nodeCount < 2)
            {
                GUI.backgroundColor = Color.cyan;
                //빈 슬롯 추가
                if(GUILayout.Button("Add Slot"))
                {
                    _neighborsProp.arraySize = nodeCount + 1;
                    SerializedProperty newElement = _neighborsProp.GetArrayElementAtIndex(_neighborsProp.arraySize-1);
                    newElement.objectReferenceValue = null;
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                GUILayout.Box("Full (Max 2 Neighbors)", GUILayout.ExpandWidth(true));
                GUI.backgroundColor = Color.white;
            }
        }

        private void CreateNextNode(RailNode current)
        {
            string currentName = current.name; 
            string newName = "RailNode_Next";

            int underscoreIndex = currentName.LastIndexOf('_');
            if(underscoreIndex>=0 && underscoreIndex < currentName.Length-1)
            {
                string suffix = currentName.Substring(underscoreIndex + 1);
                if(int.TryParse(suffix, out int number))
                    newName = $"RailNode_{number+1}"; // 5 -> 6
            }
            else
                newName = $"{currentName}_1";

            GameObject go = new GameObject(newName);
            Undo.RegisterCreatedObjectUndo(go, "Create Connected Rail Node");
            go.transform.position = current.transform.position + Vector3.right * ToolDefines.NodeEditorSpacing;
            go.transform.parent = current.transform.parent;

            RailNode newNode = go.AddComponent<RailNode>();
            Undo.RecordObject(current, "Connect Rail Node");
            Undo.RecordObject(newNode, "Connect Rail Node");
            current.ConnectTo(newNode);

            Selection.activeGameObject = go;
            EditorUtility.SetDirty(current);
        }
    }
}