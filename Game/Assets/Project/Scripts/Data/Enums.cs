namespace Project.Scripts.Data
{
    public enum UIState
    {
        None,
        Opening,
        Open,
        Closing,
        Closed
    }

    public enum UITransitionMode
    {
        None,
        Fade,
        Animation
    }
    
    public enum UILayer
    {
        HUD,
        Popup,
        System,
        Loading
    }

    /// <summary>의뢰 상태. 플래그 quest.{id} 의 값과 같습니다.</summary>
    public enum QuestState
    {
        None = 0,
        Active = 1,
        Done = 2,
        Locked = 3
    }

    /// <summary>수첩 항목 상태. 플래그 note.{id} 의 값과 같습니다. Struck = 무관 판정(취소선, 목록 하단).</summary>
    public enum NoteState
    {
        None = 0,
        Unread = 1,
        Read = 2,
        Struck = 3
    }

    /// <summary>수첩 인덱스 탭. 순서 = NotebookUI.tabButtons 순서.</summary>
    public enum NotebookTab
    {
        Cover,
        Quests,
        Residents,
        Alibis,
        Questions,
        Documents
    }

    /// <summary>시간대. 플래그 timeSlot 의 값. 시계가 아니라 스토리 비트(시퀀스 액션 setFlag:timeSlot=n)로 바뀝니다.</summary>
    public enum TimeSlot
    {
        None = 0,
        Morning = 1,
        Evening = 2,
        Night = 3
    }

    /// <summary>Sequence 테이블 type 열. SequenceData.TryGetStepType 이 소문자 이름으로 해석합니다.</summary>
    public enum SequenceStepType
    {
        Dialogue,
        Wait,
        FadeOut,
        FadeIn,
        Camera,
        Scene,
        Actions
    }

    public enum MinigameOutcomeKind
    {
        Success,
        Fail,
        Aborted
    }

    public enum MinigameRepeatPolicy
    {
        Unlimited,
        UntilSuccess,
        Once
    }

    /// <summary>미니게임 창 위치 기준.</summary>
    public enum MinigamePlacement
    {
        Center,
        Anchor,
        Source
    }

    /// <summary>미니게임 창이 열리기 시작하는 지점.</summary>
    public enum MinigameOpenFrom
    {
        WindowCenter,
        Source
    }
}
