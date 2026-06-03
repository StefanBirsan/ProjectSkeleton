namespace TheAdventure;

public class GameException : Exception
{
    public GameException(string message) : base(message) { }
    public GameException(string message, Exception inner) : base(message, inner) { }
}

public sealed class InvalidMoveException : GameException
{
    public int TargetX { get; }
    public int TargetY { get; }

    public InvalidMoveException(int x, int y)
        : base($"Cannot move to ({x},{y}).")
    {
        TargetX = x;
        TargetY = y;
    }
}

public sealed class GameOverException : GameException
{
    public int FinalScore { get; }
    public GameOverException(int score) : base("Game over.") => FinalScore = score;
}
