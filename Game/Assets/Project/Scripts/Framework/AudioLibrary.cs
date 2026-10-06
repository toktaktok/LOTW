using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Framework
{
    /// <summary>
    /// 키(클립 파일 이름, 예: BGM_Village, SFX_Door)로 오디오 클립을 찾는 목록.
    /// AudioManager에 할당하며, 대화 액션(sfx:/bgm:)처럼 데이터에서 소리를 지정할 때 사용합니다.
    /// 새 소리 추가: 클립을 Audio/{BGM|SFX}/ 에 넣고 이 에셋의 clips 목록에 끌어다 놓기.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary_New", menuName = "LOTW/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;

        private Dictionary<string, AudioClip> _lookup;

        public bool TryGetClip(string key, out AudioClip clip)
        {
            clip = null;
            if(string.IsNullOrEmpty(key))
                return false;

            if(_lookup == null)
                BuildLookup();
            return _lookup.TryGetValue(key, out clip);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AudioClip>();
            if(clips == null)
                return;

            foreach(AudioClip clip in clips)
            {
                if(clip == null)
                    continue;
                if(_lookup.ContainsKey(clip.name))
                    Debug.LogWarning($"[AudioLibrary] Duplicate clip name '{clip.name}' in {name}.");
                _lookup[clip.name] = clip;
            }
        }

        // 에디터에서 목록을 수정하면 다음 조회 때 다시 만들도록 함
        private void OnValidate() => _lookup = null;
    }
}
