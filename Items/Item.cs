namespace TheAdventure;

public enum ItemKind { HealthPotion, Sword, Shield, MagicOrb }

public interface IItem
{
    string Name { get; }
    ItemKind Kind { get; }
    string Description { get; }
}

public sealed record Item(string Name, ItemKind Kind, string Description) : IItem
{
    public static Item HealthPotion() => new("Health Potion", ItemKind.HealthPotion, "Restores 30 HP");
    public static Item Sword()        => new("Iron Sword",    ItemKind.Sword,        "+5 ATK");
    public static Item Shield()       => new("Wooden Shield", ItemKind.Shield,       "+3 DEF");
    public static Item MagicOrb()     => new("Magic Orb",     ItemKind.MagicOrb,     "Damages all nearby enemies");
}
