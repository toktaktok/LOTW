using System;
using System.Collections.Generic;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.Dialogue;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.Story
{
    /// <summary>
    /// 의뢰 조회. 상태는 플래그 quest.{id} 에 있고 변경은 대화 액션(startQuest/completeQuest)이 합니다.
    /// 변경 알림은 FlagManager.OnFlagChanged 에서 StoryKeys.IsQuestKey 로 거릅니다.
    /// </summary>
    public static class QuestLog
    {
        private static readonly IDialogueContext Context = new ManagerDialogueContext();

        public static QuestState GetState(int questId) => (QuestState)FlagManager.Instance.Get(StoryKeys.Quest(questId));

        /// <summary>진행 중 의뢰. 메인이 맨 앞.</summary>
        public static List<QuestData> GetActive() => GetByState(QuestState.Active);

        public static List<QuestData> GetByState(QuestState state)
        {
            var result = new List<QuestData>();
            foreach(QuestData quest in DataManager.Instance.GetRows<QuestData>())
            {
                if(GetState(quest.dataId) == state)
                    result.Add(quest);
            }
            SortForDisplay(result);
            return result;
        }

        public static List<QuestObjectiveData> GetObjectives(QuestData quest)
        {
            var result = new List<QuestObjectiveData>();
            if(quest?.objectiveIds == null)
                return result;

            foreach(int id in quest.objectiveIds)
            {
                QuestObjectiveData objective = DataManager.Instance.GetRow<QuestObjectiveData>(id);
                if(objective != null)
                    result.Add(objective);
            }
            return result;
        }

        public static bool IsObjectiveDone(QuestObjectiveData objective)
        {
            return IsObjectiveDone(objective, Context);
        }

        /// <summary>조건이 비어 있는 목표는 자동 완료하지 않습니다.</summary>
        public static bool IsObjectiveDone(QuestObjectiveData objective, IDialogueContext context)
        {
            if(objective == null || string.IsNullOrWhiteSpace(objective.conditions))
                return false;
            return DialogueCommands.CheckConditions(objective.conditions, context);
        }

        /// <summary>0~1. 완료된 의뢰는 1, 목표가 없으면 0.</summary>
        public static float GetProgress(QuestData quest)
        {
            if(quest == null)
                return 0f;
            if(GetState(quest.dataId) == QuestState.Done)
                return 1f;

            List<QuestObjectiveData> objectives = GetObjectives(quest);
            if(objectives.Count == 0)
                return 0f;

            int done = 0;
            foreach(QuestObjectiveData objective in objectives)
            {
                if(IsObjectiveDone(objective))
                    done++;
            }
            return (float)done / objectives.Count;
        }

        /// <summary>giverId 주민이 지금 챕터에 아직 시작하지 않은 의뢰를 갖고 있으면 true (머리 위 땀 표시).</summary>
        public static bool HasRequestFrom(string giverId)
        {
            return HasRequestFrom(giverId, FlagManager.Instance.Get(StoryKeys.Chapter), DataManager.Instance.GetRows<QuestData>(), GetState);
        }

        /// <summary>상태가 None 이고 챕터가 같은 의뢰. 잠금(Locked)/진행/완료는 제외.</summary>
        public static bool HasRequestFrom(string giverId, int chapter, IEnumerable<QuestData> quests, Func<int, QuestState> getState)
        {
            if(string.IsNullOrEmpty(giverId) || quests == null)
                return false;

            foreach(QuestData quest in quests)
            {
                if(quest.giverId == giverId && quest.chapter == chapter && getState(quest.dataId) == QuestState.None)
                    return true;
            }
            return false;
        }

        /// <summary>메인 먼저, 그다음 챕터, dataId 순.</summary>
        public static void SortForDisplay(List<QuestData> quests)
        {
            quests.Sort((a, b) =>
            {
                if(a.IsMain != b.IsMain)
                    return a.IsMain ? -1 : 1;
                if(a.chapter != b.chapter)
                    return a.chapter.CompareTo(b.chapter);
                return a.dataId.CompareTo(b.dataId);
            });
        }
    }
}
