using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// ID로 미니게임 정의를 찾는 목록. MinigameManager에 할당하며, 대화 액션(minigame:id)이 사용합니다.
    /// 새 미니게임 추가: 정의 에셋을 Data/Minigames/ 에 만들고 이 에셋의 definitions 목록에 끌어다 놓기.
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameLibrary_New", menuName = "LOTW/Minigame/Library")]
    public class MinigameLibrary : ScriptableObject
    {
        [SerializeField] private MinigameDefinition[] definitions;

        private Dictionary<string, MinigameDefinition> _lookup;

        public bool TryGetDefinition(string id, out MinigameDefinition definition)
        {
            definition = null;
            if(string.IsNullOrEmpty(id))
                return false;

            if(_lookup == null)
                BuildLookup();
            return _lookup.TryGetValue(id, out definition);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, MinigameDefinition>();
            if(definitions == null)
                return;

            foreach(MinigameDefinition definition in definitions)
            {
                if(definition == null)
                    continue;
                if(_lookup.ContainsKey(definition.Id))
                    Debug.LogWarning($"[MinigameLibrary] Duplicate minigame id '{definition.Id}' in {name}.");
                _lookup[definition.Id] = definition;
            }
        }

        // 에디터에서 목록을 수정하면 다음 조회 때 다시 만들도록 함
        private void OnValidate() => _lookup = null;
    }
}
