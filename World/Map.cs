namespace TheAdventure;

public sealed record Room(int X, int Y, int W, int H)
{
    public int CenterX => X + W / 2;
    public int CenterY => Y + H / 2;
    public bool Intersects(Room other) =>
        X < other.X + other.W && X + W > other.X &&
        Y < other.Y + other.H && Y + H > other.Y;
}

public sealed class Map
{
    public const int Width  = 50;
    public const int Height = 35;

    private readonly TileType[,] _tiles = new TileType[Width, Height];
    public EntityCollection<Enemy> Enemies { get; } = new();
    public List<Item> Items { get; } = new();
    public List<(int X, int Y)> StairsDown { get; } = new();
    public List<(int X, int Y)> StairsUp   { get; } = new();

    public TileType this[int x, int y] => InBounds(x, y) ? _tiles[x, y] : TileType.Wall;
    public bool IsWalkable(int x, int y) => this[x, y] != TileType.Wall;
    public bool InBounds(int x, int y)   => x >= 0 && x < Width && y >= 0 && y < Height;

    public static Map Generate(int floorNumber, out int startX, out int startY, Random rng)
    {
        var map = new Map();
        map.Fill(TileType.Wall);

        var rooms = new List<Room>();
        int attempts = 80;
        for (int i = 0; i < attempts; i++)
        {
            int w = rng.Next(5, 12);
            int h = rng.Next(4, 9);
            int x = rng.Next(1, Width - w - 1);
            int y = rng.Next(1, Height - h - 1);
            var room = new Room(x, y, w, h);
            if (rooms.Any(r => r.Intersects(room))) continue;
            map.CarveRoom(room);
            if (rooms.Count > 0)
                map.CarveCorridors(rooms[^1], room);
            rooms.Add(room);
        }

        if (rooms.Count == 0)
        {
            var fallback = new Room(2, 2, Width - 4, Height - 4);
            map.CarveRoom(fallback);
            rooms.Add(fallback);
        }

        startX = rooms[0].CenterX;
        startY = rooms[0].CenterY;

        var lastRoom = rooms[^1];
        map._tiles[lastRoom.CenterX, lastRoom.CenterY] = TileType.StairsDown;
        map.StairsDown.Add((lastRoom.CenterX, lastRoom.CenterY));

        if (floorNumber > 1)
        {
            map._tiles[rooms[0].CenterX + 1, rooms[0].CenterY] = TileType.StairsUp;
            map.StairsUp.Add((rooms[0].CenterX + 1, rooms[0].CenterY));
        }

        int enemyCount = 3 + floorNumber * 2;
        var candidateRooms = rooms.Skip(1).ToList();
        if (candidateRooms.Count == 0) candidateRooms = rooms;

        for (int placed = 0, tries = 0; placed < enemyCount && candidateRooms.Count > 0 && tries < enemyCount * 4; tries++)
        {
            var room = candidateRooms[rng.Next(candidateRooms.Count)];
            int ex = rng.Next(room.X + 1, room.X + room.W - 1);
            int ey = rng.Next(room.Y + 1, room.Y + room.H - 1);
            if (map.Enemies.All.Any(e => e.X == ex && e.Y == ey)) continue;
            map.Enemies.Add(EnemyFactory.Random(ex, ey, floorNumber, rng));
            placed++;
        }

        if (floorNumber == 3)
        {
            var bossRoom = rooms[rooms.Count / 2];
            map.Enemies.Add(new Boss(bossRoom.CenterX, bossRoom.CenterY));
        }

        int itemCount = 2 + floorNumber;
        for (int i = 0; i < itemCount && candidateRooms.Count > 0; i++)
        {
            var room = candidateRooms[rng.Next(candidateRooms.Count)];
            int ix = rng.Next(room.X + 1, room.X + room.W - 1);
            int iy = rng.Next(room.Y + 1, room.Y + room.H - 1);
            map.Items.Add(PickItem(rng));
            map._itemPositions.Add((ix, iy));
        }

        return map;
    }

    private readonly List<(int X, int Y)> _itemPositions = new();

    public void DropItem(Item item, int x, int y)
    {
        Items.Add(item);
        _itemPositions.Add((x, y));
    }

    public Item? TakeItemAt(int x, int y)
    {
        int idx = _itemPositions.FindIndex(p => p.X == x && p.Y == y);
        if (idx < 0) return null;
        var item = Items[idx];
        Items.RemoveAt(idx);
        _itemPositions.RemoveAt(idx);
        return item;
    }

    public bool HasItemAt(int x, int y) =>
        _itemPositions.Any(p => p.X == x && p.Y == y);

    public IEnumerable<(Item item, int X, int Y)> AllItems =>
        Items.Zip(_itemPositions, (item, pos) => (item, pos.X, pos.Y));

    private static Item PickItem(Random rng) => rng.Next(4) switch
    {
        0 => Item.HealthPotion(),
        1 => Item.Sword(),
        2 => Item.Shield(),
        _ => Item.MagicOrb()
    };

    private void Fill(TileType t)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _tiles[x, y] = t;
    }

    private void CarveRoom(Room room)
    {
        for (int x = room.X; x < room.X + room.W; x++)
            for (int y = room.Y; y < room.Y + room.H; y++)
                _tiles[x, y] = TileType.Floor;
    }

    private void CarveCorridors(Room a, Room b)
    {
        int x = a.CenterX, y = a.CenterY;
        int tx = b.CenterX, ty = b.CenterY;

        while (x != tx) { _tiles[x, y] = TileType.Floor; x += Math.Sign(tx - x); }
        while (y != ty) { _tiles[x, y] = TileType.Floor; y += Math.Sign(ty - y); }
        _tiles[x, y] = TileType.Floor;
    }
}
