using UnityEngine;
using Project.Scripts.Content.Story;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// NPC 머리 위 땀 표시. 이 주민이 지금 챕터에 아직 시작하지 않은 의뢰(Quest.giverId)를 갖고 있을 때만 보입니다.
    /// 의뢰가 시작되면(startQuest) 플래그 변경을 받아 바로 꺼집니다. PF_NPC_Base 의 자식에 둡니다.
    /// </summary>
    public class QuestMarkIndicator : MonoBehaviour
    {
        [SerializeField] private NPC npc;
        [SerializeField] private SpriteRenderer markRenderer;
        [Tooltip("번갈아 보여줄 땀 프레임")]
        [SerializeField] private Sprite[] frames;

        private bool _visible;
        private bool _dirty;
        private int _frame;
        private float _frameTimer;

        private void OnEnable()
        {
            FlagManager.OnFlagChanged += OnFlagChanged;
            Refresh();
        }

        private void OnDisable()
        {
            FlagManager.OnFlagChanged -= OnFlagChanged;
        }

        private void Update()
        {
            if(_dirty)
                Refresh();

            if(!_visible || frames == null || frames.Length < 2)
                return;

            _frameTimer += Time.deltaTime;
            if(_frameTimer < WorldDefines.QuestMarkFrameInterval)
                return;

            _frameTimer = 0f;
            _frame = (_frame + 1) % frames.Length;
            markRenderer.sprite = frames[_frame];
        }

        private void OnFlagChanged(string key)
        {
            if(StoryKeys.IsQuestKey(key) || key == StoryKeys.Chapter)
                _dirty = true;
        }

        private void Refresh()
        {
            _dirty = false;
            if(markRenderer == null)
                return;

            _visible = npc != null && QuestLog.HasRequestFrom(npc.CharacterId);
            markRenderer.enabled = _visible;
            if(_visible && frames != null && frames.Length > 0)
            {
                _frame = 0;
                _frameTimer = 0f;
                markRenderer.sprite = frames[0];
            }
        }
    }
}
