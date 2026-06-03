namespace TheAdventure;

public interface IDamageable
{
    void TakeDamage(int amount);
    bool IsAlive { get; }
}

public abstract class Entity : IDamageable
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public int Attack { get; protected set; }
    public int Defense { get; protected set; }
    public bool IsAlive => Hp > 0;

    protected Entity(int x, int y, int maxHp, int attack, int defense)
    {
        X = x; Y = y;
        MaxHp = maxHp; Hp = maxHp;
        Attack = attack; Defense = defense;
    }

    public void TakeDamage(int amount)
    {
        Hp = Math.Max(0, Hp - amount);
    }

    public void Heal(int amount)
    {
        Hp = Math.Min(MaxHp, Hp + amount);
    }

    public int DistanceTo(Entity other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}

public sealed class EntityCollection<T> where T : Entity
{
    private readonly List<T> _list = new();

    public IReadOnlyList<T> All => _list;
    public IEnumerable<T> Alive => _list.Where(e => e.IsAlive);

    public void Add(T entity) => _list.Add(entity);
    public void RemoveDead() => _list.RemoveAll(e => !e.IsAlive);
    public int Count => _list.Count;
    public T? Nearest(Entity from) =>
        Alive.OrderBy(e => e.DistanceTo(from)).FirstOrDefault();
}
