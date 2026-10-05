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
}
