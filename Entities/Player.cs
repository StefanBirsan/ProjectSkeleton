namespace TheAdventure;

public sealed class Player : Entity
{
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => Level * 20;
    public List<Item> Inventory { get; } = new();
    public int MaxInventory => 8;

    public Player(int x, int y) : base(x, y, maxHp: 50, attack: 8, defense: 3) { }

    public bool TryPickUp(Item item)
    {
        if (Inventory.Count >= MaxInventory) return false;
        Inventory.Add(item);
        return true;
    }

    public void UseItem(int index, EntityCollection<Enemy> enemies)
    {
        if (index < 0 || index >= Inventory.Count) return;
        var item = Inventory[index];
        ApplyItem(item, enemies);
        Inventory.RemoveAt(index);
    }

    private void ApplyItem(Item item, EntityCollection<Enemy> enemies)
    {
        switch (item.Kind)
        {
            case ItemKind.HealthPotion:
                Heal(30);
                break;
            case ItemKind.Sword:
                Attack += 5;
                break;
            case ItemKind.Shield:
                Defense += 3;
                break;
            case ItemKind.MagicOrb:
                foreach (var e in enemies.Alive.Where(e => DistanceTo(e) <= 3).ToList())
                    e.TakeDamage(20);
                break;
        }
    }

    public bool GainXp(int amount)
    {
        Xp += amount;
        bool leveled = false;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            LevelUp();
            leveled = true;
        }
        return leveled;
    }

    private void LevelUp()
    {
        Level++;
        MaxHp += 10;
        Hp = Math.Min(Hp + 10, MaxHp);
        Attack += 2;
        Defense += 1;
    }
}
