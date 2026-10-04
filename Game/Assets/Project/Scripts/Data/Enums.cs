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
}