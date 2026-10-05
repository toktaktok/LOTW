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