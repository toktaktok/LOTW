#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 그래프 노드 위치 파일 (Table/Layout/Dialogue.layout.json, DataId → 좌표).
    /// 기획 데이터가 아니라서 엑셀에 넣지 않고 에디터만 읽습니다. 위치가 없는 노드는 자동 배치합니다.
    /// </summary>
    public static class DialogueLayout
    {
        private static string FilePath => Path.Combine(TableWriter.TableRoot, ToolDefines.DialogueLayoutPath);

        public static Dictionary<int, Vector2> Load()
        {
            if(!File.Exists(FilePath))
                return new Dictionary<int, Vector2>();

            var file = JsonUtility.FromJson<DialogueLayoutFile>(File.ReadAllText(FilePath));
            return (file.nodes ?? new DialogueNodePosition[0]).ToDictionary(n => n.dataId, n => n.position);
        }

        /// <summary>validIds 에 있는 노드만 DataId 순서로 씁니다 (지운 노드 정리, diff 안정).</summary>
        public static void Save(Dictionary<int, Vector2> positions, ICollection<int> validIds)
        {
            var file = new DialogueLayoutFile
            {
                nodes = positions.Where(p => validIds.Contains(p.Key)).OrderBy(p => p.Key)
                    .Select(p => new DialogueNodePosition { dataId = p.Key, position = p.Value }).ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonUtility.ToJson(file, true));
        }
    }
}
#endif
