using System.Collections.Generic;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Title
{
    /// <summary>
    /// 타이틀 메뉴의 세이브 슬롯 선택과 표시 문자열. 매니저를 부르지 않는 순수 함수라 테스트에서 그대로 씁니다.
    /// </summary>
    public static class SaveSlotSelector
    {
        /// <summary>가장 최근에 저장한 슬롯. timestamp("yyyy-MM-dd HH:mm:ss")는 문자열 순서가 시간 순서. 없으면 -1.</summary>
        public static int FindLatest(IReadOnlyList<SaveData?> slots)
        {
            int latest = -1;
            string latestTime = null;
            for(int i = 0; i < slots.Count; i++)
            {
                if(!slots[i].HasValue)
                    continue;

                string time = slots[i].Value.timestamp ?? string.Empty;
                if(latest < 0 || string.CompareOrdinal(time, latestTime) > 0)
                {
                    latest = i;
                    latestTime = time;
                }
            }
            return latest;
        }

        /// <summary>새 게임 슬롯: 마지막으로 쓴 슬롯, 없으면 0. 지금은 슬롯 하나를 이어 쓰는 방식.</summary>
        public static int GetNewGameSlot(int currentSlot, int maxSlots)
        {
            return currentSlot >= 0 && currentSlot < maxSlots ? currentSlot : 0;
        }

        /// <summary>플레이 시간 "H:MM".</summary>
        public static string FormatPlayTime(float seconds)
        {
            int totalMinutes = seconds > 0f ? (int)(seconds / 60f) : 0;
            return $"{totalMinutes / 60}:{totalMinutes % 60:00}";
        }
    }
}
