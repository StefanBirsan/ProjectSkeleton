namespace TheAdventure;

public enum CombatOutcome { Miss, Graze, Hit, Critical }

public sealed record CombatResult(CombatOutcome Outcome, int Damage, string Message);

public static class CombatSystem
{
    public static CombatResult PlayerAttacks(Player player, Enemy enemy, Random rng)
    {
        int roll = rng.Next(1, 21);
        return (roll, player.Attack - enemy.Defense) switch
        {
            (20, _)         => ApplyDamage(enemy, (player.Attack - enemy.Defense + 5) * 2, CombatOutcome.Critical, $"CRITICAL! {enemy.DisplayName} hit for"),
            (1, _)          => new CombatResult(CombatOutcome.Miss, 0, $"You missed {enemy.DisplayName}!"),
            (_, <= 0)       => new CombatResult(CombatOutcome.Graze, 1, $"Grazed {enemy.DisplayName} for 1"),
            (_, int dmg)    => ApplyDamage(enemy, dmg, CombatOutcome.Hit, $"You hit {enemy.DisplayName} for"),
        };
    }

    public static CombatResult EnemyAttacks(Enemy enemy, Player player, Random rng)
    {
        int roll = rng.Next(1, 21);
        int rawDmg = enemy.Attack - player.Defense;
        return (roll, rawDmg) switch
        {
            (20, _)       => ApplyDamage(player, (rawDmg + 3) * 2, CombatOutcome.Critical, $"{enemy.DisplayName} CRITS you for"),
            (1, _)        => new CombatResult(CombatOutcome.Miss, 0, $"{enemy.DisplayName} missed!"),
            (_, <= 0)     => new CombatResult(CombatOutcome.Graze, 1, $"{enemy.DisplayName} grazed you for 1"),
            (_, int dmg)  => ApplyDamage(player, dmg, CombatOutcome.Hit, $"{enemy.DisplayName} hits you for"),
        };
    }

    private static CombatResult ApplyDamage(IDamageable target, int damage, CombatOutcome outcome, string prefix)
    {
        int actual = Math.Max(1, damage);
        target.TakeDamage(actual);
        return new CombatResult(outcome, actual, $"{prefix} {actual} damage.");
    }
}
