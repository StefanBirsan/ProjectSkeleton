namespace TheAdventure;

public enum PlayerAction
{
    None,
    MoveUp, MoveDown, MoveLeft, MoveRight,
    PickUp, UseItem1, UseItem2, UseItem3, UseItem4,
    UseItem5, UseItem6, UseItem7, UseItem8,
    ToggleInventory, Descend, Ascend,
    Confirm, Cancel,
}

public sealed class InputHandler
{
    private PlayerAction _queued = PlayerAction.None;

    public void OnKeyDown(KeyCode key)
    {
        _queued = key switch
        {
            KeyCode.Up    or KeyCode.W => PlayerAction.MoveUp,
            KeyCode.Down  or KeyCode.S => PlayerAction.MoveDown,
            KeyCode.Left  or KeyCode.A => PlayerAction.MoveLeft,
            KeyCode.Right or KeyCode.D => PlayerAction.MoveRight,
            KeyCode.G                  => PlayerAction.PickUp,
            KeyCode.One                => PlayerAction.UseItem1,
            KeyCode.Two                => PlayerAction.UseItem2,
            KeyCode.Three              => PlayerAction.UseItem3,
            KeyCode.Four               => PlayerAction.UseItem4,
            KeyCode.Five               => PlayerAction.UseItem5,
            KeyCode.Six                => PlayerAction.UseItem6,
            KeyCode.Seven              => PlayerAction.UseItem7,
            KeyCode.Eight              => PlayerAction.UseItem8,
            KeyCode.I                  => PlayerAction.ToggleInventory,
            KeyCode.Period             => PlayerAction.Descend,
            KeyCode.Comma              => PlayerAction.Ascend,
            KeyCode.Return or KeyCode.Space => PlayerAction.Confirm,
            KeyCode.Escape             => PlayerAction.Cancel,
            _                          => PlayerAction.None,
        };
    }

    public PlayerAction Consume()
    {
        var a = _queued;
        _queued = PlayerAction.None;
        return a;
    }
}
