using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 미니게임 메카닉의 기반. 메카닉 프리팹 루트에 붙이며, 프리팹은 자기 스테이지(카메라, 스프라이트 등)를 통째로 가집니다.
    /// 메카닉은 Session.Vars에 진행 상황을 쓰기만 하고, 성공/실패 판정과 보상은 정의(MinigameDefinition)가 정합니다.
    /// 규칙으로 표현하기 어려운 종료는 Session.End(name)으로 직접 요청합니다.
    /// </summary>
    public abstract class MinigameBase : MonoBehaviour
    {
        [Tooltip("스테이지를 찍는 직교 카메라. 출력은 MinigameManager가 RT로 연결")]
        [SerializeField] private Camera stageCamera;

        public Camera StageCamera => stageCamera;
        protected MinigameSession Session { get; private set; }

        public void Bind(MinigameSession session)
        {
            Session = session;
            OnBind();
        }

        /// <summary>세션이 연결된 직후 (창이 열리기 전, 스테이지 카메라와 RT는 준비됨). Session.StageResolution에 맞춰 판을 만들 때 씁니다.</summary>
        protected virtual void OnBind()
        {
        }

        /// <summary>창이 다 열리고 입력이 켜진 직후.</summary>
        public virtual void OnBegin()
        {
        }

        /// <summary>진행 중 매 프레임. 결과가 확정되면 더 이상 호출되지 않습니다.</summary>
        public virtual void OnTick(float deltaTime)
        {
        }

        /// <summary>결과 확정 직후 (창이 닫히기 전). 중단이면 outcome은 null.</summary>
        public virtual void OnEnd(MinigameOutcome outcome)
        {
        }
    }

    /// <summary>
    /// 전용 정의 타입을 쓰는 메카닉용. Definition으로 메카닉 수치를 읽습니다.
    /// </summary>
    public abstract class MinigameBase<TDefinition> : MinigameBase where TDefinition : MinigameDefinition
    {
        protected TDefinition Definition => Session.Definition as TDefinition;
    }
}
