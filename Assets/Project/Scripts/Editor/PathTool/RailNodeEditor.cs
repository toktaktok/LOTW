using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Project.Scripts.Content.World;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor
{
    [CustomEditor(typeof(RailNode))]
    public class RailNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _colorProp;
        private SerializedProperty _radiusProp;

        private void OnEnable()
        {
            _colorProp = serializedObject.FindProperty("nodeColor");
            _radiusProp = serializedObject.FindProperty("radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Color, Radius
            EditorGUILayout.PropertyField(_colorProp);
            EditorGUILayout.PropertyField(_radiusProp);
            serializedObject.ApplyModifiedProperties();

            //연결은 양쪽 노드를 함께 바꿔야 하므로 SerializedProperty가 아닌 ConnectTo/Disconnect로 직접 수정
            RailNode node = (RailNode)target;
            DrawConnections(node);

            if(node.neighbors.Count < 2)
            {
                GUILayout.Space(20);

                GUI.backgroundColor = Color.goldenRod;
                if(GUILayout.Button("Create Next Node & Connect", GUILayout.Height(40)))
                    CreateNextNode(node);
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(10);
            if(GUILayout.Button("Rename & Sort This Rail (Chain Order)"))
                ApplyRename(PlanChainRename(node));
        }

        [MenuItem("LOTW/Rail/Rename and Sort All Rail Nodes")]
        private static void RenameAllRails()
        {
            var visited = new HashSet<RailNode>();
            var plan = new List<(RailNode node, string name)>();
            foreach(RailNode node in Object.FindObjectsByType<RailNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if(visited.Contains(node))
                    continue;
                var chainPlan = PlanChainRename(node);
                foreach(var entry in chainPlan)
                    visited.Add(entry.node);
                plan.AddRange(chainPlan);
            }
            ApplyRename(plan);
        }

        /// <summary>
        /// 노드가 속한 레일 한 줄을 끝 노드부터 따라가며 {접두어}{번호}로 새 이름을 정합니다.
        /// 접두어와 시작 번호는 번호가 가장 작은 끝 노드 이름에서 가져옵니다(RailNode_A0 -> A0, A1, A2...).
        /// </summary>
        private static List<(RailNode node, string name)> PlanChainRename(RailNode any)
        {
            //연결된 노드 전체 수집
            var members = new List<RailNode>();
            var stack = new Stack<RailNode>();
            stack.Push(any);
            while(stack.Count > 0)
            {
                RailNode current = stack.Pop();
                if(current == null || members.Contains(current))
                    continue;
                members.Add(current);
                foreach(RailNode neighbor in current.neighbors)
                    stack.Push(neighbor);
            }

            //시작 끝 노드: 번호 있는 이름 우선, 번호 작은 순 (고리면 아무 노드)
            RailNode start = null;
            foreach(RailNode member in members)
            {
                if(member.neighbors.Count > 1)
                    continue;
                if(start == null || CompareStartPriority(member, start) < 0)
                    start = member;
            }
            if(start == null)
                start = any;

            (string prefix, int number) = SplitName(start.name);
            if(number < 0)
            {
                prefix = start.name + "_";
                number = 0;
            }

            var plan = new List<(RailNode node, string name)>();
            RailNode previous = null;
            RailNode walk = start;
            while(walk != null && !plan.Exists(p => p.node == walk))
            {
                plan.Add((walk, $"{prefix}{number++}"));
                RailNode next = walk.GetOtherNeighbor(previous);
                previous = walk;
                walk = next;
            }
            return plan;
        }

        private static void ApplyRename(List<(RailNode node, string name)> plan)
        {
            //대상 밖 노드와 이름이 겹치면 적용하지 않음
            var renamed = new HashSet<RailNode>();
            foreach(var entry in plan)
                renamed.Add(entry.node);
            foreach(RailNode other in Object.FindObjectsByType<RailNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if(renamed.Contains(other))
                    continue;
                if(plan.Exists(p => p.name == other.name))
                {
                    Debug.LogWarning($"[RailNodeEditor] Rename skipped: '{other.name}' already exists outside the rail.");
                    return;
                }
            }

            foreach(var entry in plan)
            {
                if(entry.node.name == entry.name)
                    continue;
                Undo.RecordObject(entry.node.gameObject, "Rename Rail Nodes");
                entry.node.gameObject.name = entry.name;
            }

            SortHierarchy(plan);
        }

        /// <summary>
        /// 같은 부모 아래 노드들을 레일 순서대로 Hierarchy에 재배치합니다.
        /// 줄의 가장 앞 노드 자리부터 연속으로 붙이고, 사이에 있던 다른 오브젝트는 줄 뒤로 밀립니다.
        /// </summary>
        private static void SortHierarchy(List<(RailNode node, string name)> plan)
        {
            var byParent = new Dictionary<Transform, List<Transform>>();
            foreach(var entry in plan)
            {
                Transform t = entry.node.transform;
                Transform key = t.parent;
                if(key == null)
                    continue; //씬 루트 노드는 정렬 대상에서 제외
                if(!byParent.TryGetValue(key, out var list))
                    byParent[key] = list = new List<Transform>();
                list.Add(t);
            }

            foreach(List<Transform> ordered in byParent.Values)
            {
                int first = int.MaxValue;
                foreach(Transform t in ordered)
                    first = Mathf.Min(first, t.GetSiblingIndex());

                //앞에서부터 채우면 앞서 배치한 노드는 다시 밀리지 않음
                for(int i = 0; i < ordered.Count; i++)
                    Undo.SetSiblingIndex(ordered[i], first + i, "Sort Rail Nodes");
            }
        }

        private static int CompareStartPriority(RailNode a, RailNode b)
        {
            int numberA = SplitName(a.name).number;
            int numberB = SplitName(b.name).number;
            if((numberA < 0) != (numberB < 0))
                return numberA < 0 ? 1 : -1;
            if(numberA != numberB)
                return numberA.CompareTo(numberB);
            return string.CompareOrdinal(a.name, b.name);
        }

        /// <summary>"RailNode_A12" -> ("RailNode_A", 12). 끝 숫자가 없으면 number = -1.</summary>
        private static (string prefix, int number) SplitName(string name)
        {
            Match match = Regex.Match(name, @"^(.*?)(\d+)$");
            return match.Success ? (match.Groups[1].Value, int.Parse(match.Groups[2].Value)) : (name, -1);
        }

        private void DrawConnections(RailNode node)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Connections (Max 2)", EditorStyles.boldLabel);

            RailNode remove = null;
            for(int index = 0; index < node.neighbors.Count; ++index)
            {
                EditorGUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField($"Slot {index + 1}", node.neighbors[index], typeof(RailNode), true);

                if(GUILayout.Button("X", GUILayout.Width(20)))
                    remove = node.neighbors[index];
                EditorGUILayout.EndHorizontal();
            }

            if(remove != null)
                ChangeConnection(node, remove, false);

            if(node.neighbors.Count < 2)
            {
                //빈 슬롯을 만들지 않고, 노드를 지정하는 순간 양방향으로 연결
                var picked = (RailNode)EditorGUILayout.ObjectField("Connect To", null, typeof(RailNode), true);
                //가득 찬 노드면 ConnectTo가 경고만 남기고 무시
                if(picked != null)
                    ChangeConnection(node, picked, true);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                GUILayout.Box("Full (Max 2 Neighbors)", GUILayout.ExpandWidth(true));
                GUI.backgroundColor = Color.white;
            }
        }

        private static void ChangeConnection(RailNode node, RailNode other, bool connect)
        {
            Undo.RecordObjects(new Object[] { node, other }, connect ? "Connect Rail Node" : "Disconnect Rail Node");
            if(connect)
                node.ConnectTo(other);
            else
                node.Disconnect(other);
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(other);
        }

        private void CreateNextNode(RailNode current)
        {
            GameObject go = new GameObject(GetNextNodeName(current.name));
            Undo.RegisterCreatedObjectUndo(go, "Create Connected Rail Node");

            //기존 이웃 반대쪽으로 이어서 생성 (이웃이 없으면 +X)
            RailNode previous = current.GetOtherNeighbor(null);
            Vector3 direction = Vector3.right;
            if(previous != null)
            {
                Vector3 away = current.transform.position - previous.transform.position;
                away.y = 0f;
                if(away.sqrMagnitude > 0.0001f)
                    direction = away.normalized;
            }
            go.transform.position = current.transform.position + direction * ToolDefines.NodeEditorSpacing;
            go.transform.parent = current.transform.parent;
            go.transform.SetSiblingIndex(current.transform.GetSiblingIndex() + 1);

            RailNode newNode = go.AddComponent<RailNode>();
            Undo.RecordObject(current, "Connect Rail Node");
            current.ConnectTo(newNode);

            Selection.activeGameObject = go;
            EditorUtility.SetDirty(current);
        }

        /// <summary>끝 숫자를 1 올린 이름(RailNode_A1 -> RailNode_A2). 씬에 이미 있으면 빈 번호까지 올립니다.</summary>
        private static string GetNextNodeName(string currentName)
        {
            (string prefix, int number) = SplitName(currentName);
            if(number < 0)
            {
                prefix = currentName + "_";
                number = 0;
            }
            number++;

            var existing = new HashSet<string>();
            foreach(RailNode node in Object.FindObjectsByType<RailNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                existing.Add(node.name);

            while(existing.Contains($"{prefix}{number}"))
                number++;
            return $"{prefix}{number}";
        }
    }
}
