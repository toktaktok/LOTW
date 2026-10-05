using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 미니게임 정의의 시작/종료 규칙 판정과 기록. 상태는 컨텍스트(플래그)로만 읽고 씁니다.
    /// 기록 플래그: mg_{id}_plays (시작 횟수), mg_{id}_cleared (성공 여부), mg_{id}_{결과이름} (결과별 횟수).
    /// </summary>
    public static class MinigameRules
    {
        /// <summary>시작 조건과 반복 정책을 모두 만족하는지.</summary>
        public static bool CanStart(MinigameDefinition definition, IDialogueContext context)
        {
            if(definition == null)
                return false;

            switch(definition.RepeatPolicy)
            {
                case MinigameRepeatPolicy.Once:
                    if(context.GetFlag(definition.PlaysFlag) > 0)
                        return false;
                    break;
                case MinigameRepeatPolicy.UntilSuccess:
                    if(context.GetFlag(definition.ClearedFlag) > 0)
                        return false;
                    break;
            }
            return DialogueCommands.CheckConditions(definition.StartConditions, context);
        }

        /// <summary>when 조건을 만족하는 첫 결과. when이 빈 결과는 건너뜁니다. 없으면 null.</summary>
        public static MinigameOutcome Evaluate(MinigameOutcome[] outcomes, IDialogueContext context)
        {
            if(outcomes == null)
                return null;

            foreach(MinigameOutcome outcome in outcomes)
            {
                if(!string.IsNullOrWhiteSpace(outcome.when) && DialogueCommands.CheckConditions(outcome.when, context))
                    return outcome;
            }
            return null;
        }

        public static MinigameOutcome Find(MinigameOutcome[] outcomes, string name)
        {
            if(outcomes == null)
                return null;

            foreach(MinigameOutcome outcome in outcomes)
            {
                if(outcome.name == name)
                    return outcome;
            }
            return null;
        }

        /// <summary>시작 액션을 실행하고 시작 횟수를 기록합니다.</summary>
        public static void ApplyStart(MinigameDefinition definition, IDialogueContext context)
        {
            DialogueCommands.RunActions(definition.StartActions, context);
            context.AddFlag(definition.PlaysFlag, 1);
        }

        /// <summary>결과 액션을 실행하고 기록합니다. outcome이 null이면 중단으로 처리합니다.</summary>
        public static MinigameResult ApplyEnd(MinigameDefinition definition, MinigameOutcome outcome, IDialogueContext context)
        {
            if(outcome == null)
            {
                DialogueCommands.RunActions(definition.AbortActions, context);
                return new MinigameResult
                {
                    minigameId = definition.Id,
                    outcomeName = MinigameDefines.AbortedOutcomeName,
                    kind = MinigameOutcomeKind.Aborted,
                    followDialogueId = -1
                };
            }

            DialogueCommands.RunActions(outcome.actions, context);
            if(!string.IsNullOrEmpty(outcome.name))
                context.AddFlag(definition.GetOutcomeFlag(outcome.name), 1);
            if(outcome.kind == MinigameOutcomeKind.Success)
                context.SetFlag(definition.ClearedFlag, 1);

            return new MinigameResult
            {
                minigameId = definition.Id,
                outcomeName = outcome.name,
                kind = outcome.kind,
                followDialogueId = outcome.followDialogueId
            };
        }

        /// <summary>
        /// 창 중심 위치를 화면 안으로 밀어 넣습니다. 좌표는 화면 중심이 원점인 캔버스 단위.
        /// 창이 화면보다 크면 가운데에 둡니다.
        /// </summary>
        public static Vector2 ClampToScreen(Vector2 center, Vector2 windowSize, Vector2 screenSize)
        {
            Vector2 limit = Vector2.Max((screenSize - windowSize) * 0.5f, Vector2.zero);
            return new Vector2(Mathf.Clamp(center.x, -limit.x, limit.x), Mathf.Clamp(center.y, -limit.y, limit.y));
        }
    }
}
