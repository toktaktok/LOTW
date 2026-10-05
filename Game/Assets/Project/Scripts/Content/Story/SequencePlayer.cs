using Unity.Cinemachine;
using UnityEngine;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.Content.UI;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Content.Story
{
    /// <summary>
    /// Sequence 테이블 연출(대화/대기/페이드/카메라/씬 이동/액션)을 순서대로 실행합니다.
    /// 씬이 바뀌어도 이어지도록 DontDestroyOnLoad 싱글턴. 재생 중에는 플레이어 이동/상호작용/수첩 버튼이 막힙니다.
    /// 시작: 대화 액션 sequence:id, SequenceTrigger, 새 게임(프롤로그).
    /// </summary>
    public class SequencePlayer : Singleton<SequencePlayer>
    {
        private readonly IDialogueContext _context = new ManagerDialogueContext();

        public static bool IsPlaying { get; private set; }

        public void Play(int startId)
        {
            if(IsPlaying)
            {
                Debug.LogWarning($"[SequencePlayer] Sequence already playing, ignored {startId}");
                return;
            }
            RunAsync(startId).Forget();
        }

        private async Awaitable RunAsync(int startId)
        {
            IsPlaying = true;
            try
            {
                // sequence: 액션으로 시작했으면 그 대화가 닫힐 때까지 기다림
                while(UIManager.Instance.HasBlockingPage)
                    await Awaitable.NextFrameAsync();

                int id = startId;
                for(int count = 0; id > 0; count++)
                {
                    if(count >= StoryDefines.MaxSequenceSteps)
                    {
                        Debug.LogWarning($"[SequencePlayer] Too many steps from {startId}, stopped at {id}");
                        break;
                    }

                    SequenceData step = DataManager.Instance.GetRow<SequenceData>(id);
                    if(step == null)
                    {
                        Debug.LogWarning($"[SequencePlayer] Step {id} not found");
                        break;
                    }

                    if(DialogueCommands.CheckConditions(step.conditions, _context))
                        await RunStepAsync(step);
                    id = step.nextId;
                }
            }
            finally
            {
                IsPlaying = false;
            }
        }

        private async Awaitable RunStepAsync(SequenceData step)
        {
            if(!step.TryGetStepType(out SequenceStepType type))
            {
                Debug.LogWarning($"[SequencePlayer] Unknown step type '{step.type}' in {step.dataId}");
                return;
            }

            switch(type)
            {
                case SequenceStepType.Dialogue:
                    await PlayDialogueAsync(step);
                    break;
                case SequenceStepType.Wait:
                    await Awaitable.WaitForSecondsAsync(step.duration);
                    break;
                case SequenceStepType.FadeOut:
                    await SceneTransitionManager.Instance.FadeAsync(1f, FadeDuration(step));
                    break;
                case SequenceStepType.FadeIn:
                    await SceneTransitionManager.Instance.FadeAsync(0f, FadeDuration(step));
                    break;
                case SequenceStepType.Camera:
                    SwitchCamera(step);
                    if(step.duration > 0f)
                        await Awaitable.WaitForSecondsAsync(step.duration);
                    break;
                case SequenceStepType.Scene:
                    await LoadSceneAsync(step.param);
                    break;
                case SequenceStepType.Actions:
                    DialogueCommands.RunActions(step.param, _context);
                    break;
            }
        }

        private static float FadeDuration(SequenceData step)
        {
            return step.duration > 0f ? step.duration : StoryDefines.SequenceFadeDuration;
        }

        private static async Awaitable PlayDialogueAsync(SequenceData step)
        {
            DialogueData start = int.TryParse(step.param, out int dialogueId) ? DataManager.Instance.GetRow<DialogueData>(dialogueId) : null;
            if(start == null)
            {
                Debug.LogWarning($"[SequencePlayer] Dialogue '{step.param}' not found in step {step.dataId}");
                return;
            }

            bool finished = false;
            bool opened = false;
            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.SetupDialogue(start, null, null, () => finished = true));

            // 씬 전환 등으로 콜백 없이 닫혀도 멈추지 않도록 열림 -> 닫힘도 끝으로 봄.
            // minigame: 행은 대화를 닫고 미니게임 뒤 다시 열므로, 미니게임/UI 처리 중의 닫힘은 끝이 아님
            while(!finished)
            {
                bool open = UIManager.Instance.IsOpen<DialogueUI>();
                if(opened && !open && !MinigameManager.Instance.IsPlaying && !UIManager.Instance.HasBlockingPage)
                    break;
                opened |= open;
                await Awaitable.NextFrameAsync();
            }

            while(UIManager.Instance.HasBlockingPage)
                await Awaitable.NextFrameAsync();
        }

        private static void SwitchCamera(SequenceData step)
        {
            GameObject target = GameObject.Find(step.param);
            CinemachineCamera camera = target != null ? target.GetComponent<CinemachineCamera>() : null;
            if(camera == null)
            {
                Debug.LogWarning($"[SequencePlayer] CinemachineCamera '{step.param}' not found in step {step.dataId}");
                return;
            }
            CameraManager.Instance.SwitchCamera(camera, step.duration > 0f ? step.duration : -1f);
        }

        private static async Awaitable LoadSceneAsync(string param)
        {
            string sceneName = param;
            string entranceId = string.Empty;
            int sep = param.IndexOf(':');
            if(sep >= 0)
            {
                sceneName = param.Substring(0, sep);
                entranceId = param.Substring(sep + 1);
            }

            SceneTransitionManager transitions = SceneTransitionManager.Instance;
            while(transitions.IsTransitioning)
                await Awaitable.NextFrameAsync();

            transitions.TransitionTo(sceneName, entranceId);
            while(transitions.IsTransitioning)
                await Awaitable.NextFrameAsync();
        }
    }
}
