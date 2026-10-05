#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 에디터에서 대화를 한 단계씩 진행합니다. 진행 규칙은 DialogueUI 와 같습니다:
    ///   표시: 분기 행을 풀고(ResolveRoute), 행의 actions 실행, 조건을 만족하는 choiceIds 를 선택지로 제시
    ///   선택: 선택지 행의 actions 실행 후 그 행의 nextId 로
    ///   진행: 고를 수 있는 선택지가 없으면 nextId 로
    /// 뒤로 가기는 시작 상태부터 입력 기록을 다시 재생합니다.
    /// DialogueUI 의 진행 규칙을 바꾸면 여기도 같이 바꿉니다.
    /// </summary>
    public class DialogueSimulator
    {
        // 입력 기록에서 "다음으로 진행"을 뜻하는 값 (선택은 선택지 dataId)
        private const int AdvanceInput = -1;
        private static readonly Regex ConditionKeyPattern = new(@"(flag|item)\s*:\s*([^<>=!;\s]+)", RegexOptions.IgnoreCase);

        private readonly Func<int, DialogueData> _getLine;
        private readonly List<int> _inputs = new();
        private readonly List<DialogueSimChoice> _choices = new();

        public DialogueSimulator(Func<int, DialogueData> getLine)
        {
            _getLine = id => id < 0 ? null : getLine(id);
        }

        /// <summary>시작할 때의 플래그. 바꾼 뒤 Restart 로 반영합니다.</summary>
        public Dictionary<string, int> StartFlags { get; } = new();
        public Dictionary<string, int> StartItems { get; } = new();

        public int StartId { get; private set; } = AdvanceInput;
        public SimDialogueContext Context { get; private set; }
        public DialogueData Current { get; private set; }
        public IReadOnlyList<DialogueSimChoice> Choices => _choices;
        public bool IsRunning => Context != null;
        public bool IsFinished => IsRunning && Current == null;
        public bool CanStepBack => _inputs.Count > 0;

        public bool HasAvailableChoice
        {
            get
            {
                foreach(DialogueSimChoice choice in _choices)
                {
                    if(choice.IsAvailable)
                        return true;
                }
                return false;
            }
        }

        public void Start(int startId)
        {
            StartId = startId;
            _inputs.Clear();
            Replay();
        }

        public void Restart()
        {
            if(StartId >= 0)
                Start(StartId);
        }

        public void Advance()
        {
            if(IsFinished || !IsRunning || HasAvailableChoice)
                return;

            _inputs.Add(AdvanceInput);
            Apply(AdvanceInput);
        }

        public void Choose(int choiceId)
        {
            if(!_choices.Exists(c => c.dataId == choiceId && c.IsAvailable))
                return;

            _inputs.Add(choiceId);
            Apply(choiceId);
        }

        public void StepBack()
        {
            if(!CanStepBack)
                return;

            _inputs.RemoveAt(_inputs.Count - 1);
            Replay();
        }

        /// <summary>conditions 에 쓰인 플래그와 아이템 키를 모읍니다. 시작 상태를 미리 채울 때 씁니다.</summary>
        public static void CollectConditionKeys(IEnumerable<DialogueData> lines, ISet<string> flags, ISet<string> items)
        {
            foreach(DialogueData line in lines)
            {
                if(string.IsNullOrEmpty(line.conditions))
                    continue;

                foreach(Match match in ConditionKeyPattern.Matches(line.conditions))
                {
                    bool isFlag = match.Groups[1].Value.Equals("flag", StringComparison.OrdinalIgnoreCase);
                    (isFlag ? flags : items).Add(match.Groups[2].Value);
                }
            }
        }

        private void Replay()
        {
            Context = new SimDialogueContext(StartFlags, StartItems);
            Show(_getLine(StartId), StartId);
            foreach(int input in _inputs)
                Apply(input);
        }

        private void Apply(int input)
        {
            if(input == AdvanceInput)
            {
                Show(_getLine(Current.nextId), Current.nextId);
                return;
            }

            DialogueData choice = _getLine(input);
            Context.AddLog($"[{input}] 선택");
            DialogueCommands.RunActions(choice.actions, Context);
            Show(_getLine(choice.nextId), choice.nextId);
        }

        private void Show(DialogueData line, int requestedId)
        {
            _choices.Clear();

            if(line == null && requestedId >= 0)
                Context.AddLog($"[{requestedId}] 행이 없습니다");

            DialogueData shown = DialogueCommands.ResolveRoute(line, _getLine, Context);
            if(shown != line && line != null)
                Context.AddLog($"[{line.dataId}] 분기 -> {(shown != null ? shown.dataId.ToString() : "종료")}");

            Current = shown;
            if(shown == null)
            {
                Context.AddLog("대화 종료");
                return;
            }

            Context.AddLog($"[{shown.dataId}] 표시");
            DialogueCommands.RunActions(shown.actions, Context);

            if(shown.choiceIds == null)
                return;

            foreach(int choiceId in shown.choiceIds)
            {
                DialogueData choice = _getLine(choiceId);
                string blockedBy = choice == null ? "행이 없습니다" : GetFailedCondition(choice.conditions);
                _choices.Add(new DialogueSimChoice(choiceId, choice, blockedBy));
            }
        }

        /// <summary>만족하지 못한 첫 조건 문자열. 모두 만족하면 null.</summary>
        private string GetFailedCondition(string conditions)
        {
            if(string.IsNullOrWhiteSpace(conditions))
                return null;

            foreach(string raw in conditions.Split(DialogueCommands.Separator))
            {
                string token = raw.Trim();
                if(token.Length > 0 && !DialogueCommands.CheckConditions(token, Context))
                    return token;
            }
            return null;
        }
    }
}
#endif
