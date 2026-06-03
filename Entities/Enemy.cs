namespace TheAdventure;

public enum EnemyKind { Goblin, Troll, Boss }

public abstract class Enemy : Entity
{
    public string DisplayName { get; }
    public EnemyKind Kind { get; }
    public int XpReward { get; }

    protected Enemy(int x, int y, string name, EnemyKind kind, int maxHp, int attack, int defense, int xp)
        : base(x, y, maxHp, attack, defense)
    {
        DisplayName = name;
        Kind = kind;
        XpReward = xp;
    }

    public (int dx, int dy) GetMoveToward(Player player, Random rng)
    {
        int dist = DistanceTo(player);

        if (dist > 8)
        {
            int dir = rng.Next(4);
            return dir switch { 0 => (0, -1), 1 => (0, 1), 2 => (-1, 0), _ => (1, 0) };
        }

        int dx = Math.Sign(player.X - X);
        int dy = Math.Sign(player.Y - Y);

        if (Math.Abs(player.X - X) >= Math.Abs(player.Y - Y))
            return (dx, 0);
        return (0, dy);
    }
}

public sealed class Goblin : Enemy
{
    public Goblin(int x, int y) : base(x, y, "Goblin", EnemyKind.Goblin, 15, 5, 1, 10) { }
}

public sealed class Troll : Enemy
{
    public Troll(int x, int y) : base(x, y, "Troll", EnemyKind.Troll, 35, 10, 4, 25) { }
}

public sealed class Boss : Enemy
{
    public Boss(int x, int y) : base(x, y, "DUNGEON BOSS", EnemyKind.Boss, 120, 18, 8, 200) { }
}

public static class EnemyFactory
{
    public static Enemy Create(EnemyKind kind, int x, int y) => kind switch
    {
        EnemyKind.Goblin => new Goblin(x, y),
        EnemyKind.Troll  => new Troll(x, y),
        EnemyKind.Boss   => new Boss(x, y),
        _ => throw new GameException($"Unknown enemy kind: {kind}")
    };

    public static Enemy Random(int x, int y, int floor, System.Random rng)
    {
        double roll = rng.NextDouble();
        return floor switch
        {
            1 => Create(EnemyKind.Goblin, x, y),
            2 => roll < 0.5 ? Create(EnemyKind.Goblin, x, y) : Create(EnemyKind.Troll, x, y),
            _ => roll < 0.3 ? Create(EnemyKind.Goblin, x, y) : Create(EnemyKind.Troll, x, y)
        };
    }
}
