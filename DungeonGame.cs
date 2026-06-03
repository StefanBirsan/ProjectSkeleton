using Silk.NET.SDL;

namespace TheAdventure;

public enum GamePhase { MainMenu, Playing, Inventory, GameOver, Victory }

public sealed class DungeonGame : IDisposable
{
    private const int TileSize   = 24;
    private const int MapCols    = 33;
    private const int MapRows    = 25;
    private const int HudHeight  = 200;
    private const int WinW       = MapCols * TileSize;
    private const int WinH       = MapRows * TileSize + HudHeight;

    private readonly Sdl _sdl;
    private readonly IntPtr _window;
    private readonly IntPtr _rendererHandle;
    private readonly GameRenderer _renderer;
    private readonly InputHandler _input = new();
    private readonly HighScoreManager _scores = new();
    private readonly Random _rng = new();

    private GamePhase _phase = GamePhase.MainMenu;
    private Map _map = null!;
    private Player _player = null!;
    private int _floor = 1;
    private int _score;
    private string _lastMessage = "Welcome to The Adventure!";
    private bool _quit;
    private bool _bossDefeated;

    public DungeonGame()
    {
        _sdl = new Sdl(new SdlContext());

        int initResult = _sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer);
        if (initResult < 0)
            throw new GameException("SDL init failed.");

        unsafe
        {
            _window = (IntPtr)_sdl.CreateWindow(
                "The Adventure - Dungeon Roguelite",
                Sdl.WindowposUndefined, Sdl.WindowposUndefined,
                WinW, WinH,
                (uint)WindowFlags.Resizable
            );
        }
        if (_window == IntPtr.Zero)
            throw new GameException("Window creation failed.");

        unsafe
        {
            _rendererHandle = (IntPtr)_sdl.CreateRenderer(
                (Window*)_window, -1, (uint)RendererFlags.Accelerated);
            _sdl.RenderSetVSync((Renderer*)_rendererHandle, 1);
        }
        if (_rendererHandle == IntPtr.Zero)
            throw new GameException("Renderer creation failed.");

        _renderer = new GameRenderer(_sdl, _rendererHandle, WinW, WinH);
    }

    public async Task RunAsync()
    {
        await _scores.LoadAsync();
        var ev = new Event();

        while (!_quit)
        {
            while (_sdl.PollEvent(ref ev) != 0)
            {
                switch (ev.Type)
                {
                    case (uint)EventType.Quit:
                        _quit = true;
                        break;
                    case (uint)EventType.Keydown:
                        _input.OnKeyDown((KeyCode)ev.Key.Keysym.Scancode);
                        break;
                }
            }

            var action = _input.Consume();

            switch (_phase)
            {
                case GamePhase.MainMenu:
                    UpdateMenu(action);
                    break;
                case GamePhase.Playing:
                    await UpdatePlayingAsync(action);
                    break;
                case GamePhase.Inventory:
                    UpdateInventory(action);
                    break;
                case GamePhase.GameOver:
                case GamePhase.Victory:
                    if (action == PlayerAction.Confirm || action == PlayerAction.Cancel)
                        _phase = GamePhase.MainMenu;
                    break;
            }

            _renderer.Clear(10, 10, 15);
            switch (_phase)
            {
                case GamePhase.MainMenu: RenderMenu(); break;
                case GamePhase.Playing:  RenderPlaying(); break;
                case GamePhase.Inventory: RenderPlaying(); RenderInventoryOverlay(); break;
                case GamePhase.GameOver: RenderGameOver(); break;
                case GamePhase.Victory:  RenderVictory(); break;
            }
            _renderer.Present();
        }
    }

    private void UpdateMenu(PlayerAction action)
    {
        if (action == PlayerAction.Confirm)
            StartNewGame();
    }

    private void StartNewGame()
    {
        _floor = 1;
        _score = 0;
        _bossDefeated = false;
        _map = Map.Generate(_floor, out int sx, out int sy, _rng);
        _player = new Player(sx, sy);
        _lastMessage = "You descend into the dungeon...";
        _phase = GamePhase.Playing;
    }

    private void RenderMenu()
    {
        int cx = WinW / 2;
        DrawCentered("THE ADVENTURE", cx, 120, 255, 200, 50, 3);
        DrawCentered("DUNGEON ROGUELITE", cx, 160, 200, 160, 40, 2);
        DrawCentered("PRESS ENTER TO START", cx, 240, 200, 200, 200, 2);
        DrawCentered("WASD/ARROWS  MOVE", cx, 310, 160, 160, 160, 2);
        DrawCentered("G  PICK UP ITEM", cx, 332, 160, 160, 160, 2);
        DrawCentered("1-8  USE ITEM", cx, 354, 160, 160, 160, 2);
        DrawCentered(". / COMMA  STAIRS", cx, 376, 160, 160, 160, 2);
        DrawCentered("I  INVENTORY", cx, 398, 160, 160, 160, 2);

        DrawCentered("HIGH SCORES", cx, 450, 255, 215, 80, 2);
        var top = _scores.TopScores.Take(5).ToList();
        for (int i = 0; i < top.Count; i++)
        {
            var e = top[i];
            DrawCentered($"{i + 1}. {e.Name}  {e.Score}  FL{e.Floor}", cx, 476 + i * 22, 180, 220, 180, 2);
        }
        if (top.Count == 0)
            DrawCentered("NO SCORES YET", cx, 476, 120, 120, 120, 2);
    }

    private async Task UpdatePlayingAsync(PlayerAction action)
    {
        if (action == PlayerAction.None) return;

        if (action == PlayerAction.ToggleInventory) { _phase = GamePhase.Inventory; return; }
        if (action == PlayerAction.Cancel) { _phase = GamePhase.MainMenu; return; }

        bool playerActed = false;

        switch (action)
        {
            case PlayerAction.MoveUp:    playerActed = TryMove(0, -1); break;
            case PlayerAction.MoveDown:  playerActed = TryMove(0,  1); break;
            case PlayerAction.MoveLeft:  playerActed = TryMove(-1, 0); break;
            case PlayerAction.MoveRight: playerActed = TryMove( 1, 0); break;
            case PlayerAction.PickUp:    playerActed = TryPickUp(); break;
            case PlayerAction.Descend:   playerActed = TryDescend(); break;
            case PlayerAction.Ascend:    playerActed = TryAscend(); break;
            case PlayerAction.UseItem1:  playerActed = TryUseItem(0); break;
            case PlayerAction.UseItem2:  playerActed = TryUseItem(1); break;
            case PlayerAction.UseItem3:  playerActed = TryUseItem(2); break;
            case PlayerAction.UseItem4:  playerActed = TryUseItem(3); break;
            case PlayerAction.UseItem5:  playerActed = TryUseItem(4); break;
            case PlayerAction.UseItem6:  playerActed = TryUseItem(5); break;
            case PlayerAction.UseItem7:  playerActed = TryUseItem(6); break;
            case PlayerAction.UseItem8:  playerActed = TryUseItem(7); break;
        }

        if (playerActed)
        {
            RunEnemyTurns();
            _map.Enemies.RemoveDead();
            CheckWinLose();
            if (_phase == GamePhase.GameOver)
                await RecordScoreAsync();
        }
    }

    private bool TryMove(int dx, int dy)
    {
        int nx = _player.X + dx, ny = _player.Y + dy;

        var target = _map.Enemies.Alive.FirstOrDefault(e => e.X == nx && e.Y == ny);
        if (target != null)
        {
            var result = CombatSystem.PlayerAttacks(_player, target, _rng);
            _lastMessage = result.Message;
            if (!target.IsAlive)
            {
                _score += target.XpReward;
                _player.GainXp(target.XpReward);
                if (target.Kind == EnemyKind.Boss) _bossDefeated = true;
                _lastMessage += $" {target.DisplayName} defeated!";
            }
            return true;
        }

        if (!_map.IsWalkable(nx, ny)) { _lastMessage = "Blocked."; return false; }

        _player.X = nx;
        _player.Y = ny;

        if (_map[nx, ny] == TileType.StairsDown) _lastMessage = "Press '.' to descend.";
        else if (_map[nx, ny] == TileType.StairsUp) _lastMessage = "Press ',' to ascend.";
        else if (_map.HasItemAt(nx, ny)) _lastMessage = "Press 'G' to pick up.";
        else _lastMessage = string.Empty;

        return true;
    }

    private bool TryPickUp()
    {
        var item = _map.TakeItemAt(_player.X, _player.Y);
        if (item == null) { _lastMessage = "Nothing here."; return false; }
        if (_player.TryPickUp(item)) { _lastMessage = $"Picked up {item.Name}."; return true; }
        _map.Items.Add(item);
        _map._itemPositions.Add((_player.X, _player.Y));
        _lastMessage = "Inventory full!";
        return false;
    }

    private bool TryDescend()
    {
        if (_map[_player.X, _player.Y] != TileType.StairsDown)
        { _lastMessage = "No stairs here."; return false; }
        if (_floor >= 3) { _lastMessage = "Deepest floor."; return false; }
        _floor++;
        _score += _floor * 50;
        _map = Map.Generate(_floor, out int sx, out int sy, _rng);
        _player.X = sx; _player.Y = sy;
        _lastMessage = $"You descend to floor {_floor}!";
        return true;
    }

    private bool TryAscend()
    {
        if (_map[_player.X, _player.Y] != TileType.StairsUp)
        { _lastMessage = "No stairs here."; return false; }
        if (_floor <= 1) { _lastMessage = "Already at top."; return false; }
        _floor--;
        _map = Map.Generate(_floor, out int sx, out int sy, _rng);
        _player.X = sx; _player.Y = sy;
        _lastMessage = $"You ascend to floor {_floor}.";
        return true;
    }

    private bool TryUseItem(int index)
    {
        if (index >= _player.Inventory.Count) { _lastMessage = "No item there."; return false; }
        var name = _player.Inventory[index].Name;
        _player.UseItem(index, _map.Enemies);
        _lastMessage = $"Used {name}.";
        return true;
    }

    private void RunEnemyTurns()
    {
        foreach (var enemy in _map.Enemies.Alive.ToList())
        {
            if (!enemy.IsAlive) continue;

            if (enemy.DistanceTo(_player) == 1)
            {
                var result = CombatSystem.EnemyAttacks(enemy, _player, _rng);
                if (string.IsNullOrEmpty(_lastMessage) || _lastMessage == string.Empty)
                    _lastMessage = result.Message;
            }
            else
            {
                var (dx, dy) = enemy.GetMoveToward(_player, _rng);
                int nx = enemy.X + dx, ny = enemy.Y + dy;
                bool occupied = _map.Enemies.Alive.Any(e => e != enemy && e.X == nx && e.Y == ny)
                             || (_player.X == nx && _player.Y == ny);
                if (_map.IsWalkable(nx, ny) && !occupied)
                {
                    enemy.X = nx;
                    enemy.Y = ny;
                }
            }
        }
    }

    private void CheckWinLose()
    {
        if (!_player.IsAlive) { _phase = GamePhase.GameOver; return; }
        if (_bossDefeated)    { _phase = GamePhase.Victory;  _score += 500; }
    }

    private async Task RecordScoreAsync()
    {
        var entry = new HighScoreEntry("HERO", _score, _floor, DateTime.Now.ToString("yyyy-MM-dd"));
        await _scores.AddAsync(entry);
    }

    private void UpdateInventory(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.ToggleInventory or PlayerAction.Cancel:
                _phase = GamePhase.Playing; break;
            case PlayerAction.UseItem1: UseAndReturn(0); break;
            case PlayerAction.UseItem2: UseAndReturn(1); break;
            case PlayerAction.UseItem3: UseAndReturn(2); break;
            case PlayerAction.UseItem4: UseAndReturn(3); break;
            case PlayerAction.UseItem5: UseAndReturn(4); break;
            case PlayerAction.UseItem6: UseAndReturn(5); break;
            case PlayerAction.UseItem7: UseAndReturn(6); break;
            case PlayerAction.UseItem8: UseAndReturn(7); break;
        }
    }

    private void UseAndReturn(int idx)
    {
        TryUseItem(idx);
        _phase = GamePhase.Playing;
    }

    private void RenderPlaying()
    {
        int vpX = Math.Clamp(_player.X - MapCols / 2, 0, Map.Width  - MapCols);
        int vpY = Math.Clamp(_player.Y - MapRows / 2, 0, Map.Height - MapRows);

        for (int ty = 0; ty < MapRows; ty++)
        {
            for (int tx = 0; tx < MapCols; tx++)
            {
                int mx = vpX + tx, my = vpY + ty;
                int px = tx * TileSize, py = ty * TileSize;
                TileColor(_map[mx, my], out byte r, out byte g, out byte b);
                _renderer.FillRect(px, py, TileSize, TileSize, r, g, b);

                if (_map[mx, my] != TileType.Wall)
                    _renderer.DrawRect(px, py, TileSize, TileSize, 30, 30, 35);
            }
        }

        foreach (var (_, ix, iy) in _map.AllItems)
        {
            int tx2 = ix - vpX, ty2 = iy - vpY;
            if (tx2 < 0 || tx2 >= MapCols || ty2 < 0 || ty2 >= MapRows) continue;
            _renderer.FillRect(tx2 * TileSize + 6, ty2 * TileSize + 6, TileSize - 12, TileSize - 12, 255, 215, 0);
        }

        foreach (var enemy in _map.Enemies.Alive)
        {
            int tx2 = enemy.X - vpX, ty2 = enemy.Y - vpY;
            if (tx2 < 0 || tx2 >= MapCols || ty2 < 0 || ty2 >= MapRows) continue;
            EnemyColor(enemy.Kind, out byte er, out byte eg, out byte eb);
            _renderer.FillRect(tx2 * TileSize + 3, ty2 * TileSize + 3, TileSize - 6, TileSize - 6, er, eg, eb);
            int barW = (int)((float)enemy.Hp / enemy.MaxHp * (TileSize - 6));
            _renderer.FillRect(tx2 * TileSize + 3, ty2 * TileSize + 1, barW, 2, 255, 50, 50);
        }

        {
            int tx2 = _player.X - vpX, ty2 = _player.Y - vpY;
            _renderer.FillRect(tx2 * TileSize + 2, ty2 * TileSize + 2, TileSize - 4, TileSize - 4, 80, 160, 255);
            int barW = (int)((float)_player.Hp / _player.MaxHp * (TileSize - 4));
            _renderer.FillRect(tx2 * TileSize + 2, ty2 * TileSize, barW, 2, 50, 255, 50);
        }

        RenderHud(MapRows * TileSize);
    }

    private void RenderHud(int hudY)
    {
        _renderer.FillRect(0, hudY, WinW, HudHeight, 15, 15, 25);
        _renderer.DrawLine(0, hudY, WinW, hudY, 80, 80, 100);

        int y1 = hudY + 10, y2 = hudY + 32, y3 = hudY + 54;

        _renderer.DrawText($"HP:{_player.Hp}/{_player.MaxHp}", 10, y1, 50, 220, 50, 2);
        _renderer.DrawText($"ATK:{_player.Attack}", 190, y1, 220, 100, 50, 2);
        _renderer.DrawText($"DEF:{_player.Defense}", 320, y1, 50, 100, 220, 2);
        _renderer.DrawText($"LVL:{_player.Level}", 440, y1, 220, 220, 50, 2);
        _renderer.DrawText($"XP:{_player.Xp}/{_player.XpToNext}", 550, y1, 160, 160, 220, 2);

        _renderer.DrawText($"FLOOR:{_floor}/3", 10, y2, 200, 180, 100, 2);
        _renderer.DrawText($"SCORE:{_score}", 200, y2, 255, 215, 0, 2);
        _renderer.DrawText($"INV:{_player.Inventory.Count}/{_player.MaxInventory}", 500, y2, 180, 180, 180, 2);

        for (int i = 0; i < _player.Inventory.Count && i < 8; i++)
        {
            int ix = 10 + i * 95;
            _renderer.FillRect(ix, y3, 88, 18, 30, 30, 50);
            _renderer.DrawRect(ix, y3, 88, 18, 80, 80, 120);
            _renderer.DrawText($"{i + 1}:{_player.Inventory[i].Name[..Math.Min(6, _player.Inventory[i].Name.Length)]}", ix + 2, y3 + 2, 220, 220, 180, 1);
        }

        if (!string.IsNullOrEmpty(_lastMessage))
        {
            int msgY = hudY + 80;
            _renderer.FillRect(0, msgY, WinW, 22, 10, 10, 20);
            _renderer.DrawText(_lastMessage, 10, msgY + 3, 220, 220, 100, 2);
        }

        int legY = hudY + 108;
        _renderer.DrawText("WASD:MOVE  G:PICKUP  1-8:USE  .:DOWN  ,:UP  I:INV  ESC:MENU", 10, legY, 100, 100, 120, 1);

        int hpBarY = hudY + 130;
        int hpBarW = (int)((float)_player.Hp / _player.MaxHp * 300);
        _renderer.FillRect(10, hpBarY, 300, 12, 60, 20, 20);
        _renderer.FillRect(10, hpBarY, hpBarW, 12, (byte)Math.Min(255, 50 + hpBarW / 2), 200, 50);
        _renderer.DrawRect(10, hpBarY, 300, 12, 120, 120, 140);
    }

    private void RenderInventoryOverlay()
    {
        int ox = WinW / 2 - 200, oy = 80;
        _renderer.FillRect(ox, oy, 400, 380, 10, 10, 30);
        _renderer.DrawRect(ox, oy, 400, 380, 120, 120, 200);
        DrawCentered("INVENTORY", WinW / 2, oy + 12, 255, 215, 80, 2);
        DrawCentered("PRESS NUMBER TO USE  I/ESC TO CLOSE", WinW / 2, oy + 32, 160, 160, 160, 1);

        for (int i = 0; i < _player.Inventory.Count; i++)
        {
            var item = _player.Inventory[i];
            int ry = oy + 60 + i * 34;
            _renderer.FillRect(ox + 10, ry, 380, 28, 20, 20, 45);
            _renderer.DrawRect(ox + 10, ry, 380, 28, 70, 70, 120);
            _renderer.DrawText($"{i + 1}. {item.Name}", ox + 18, ry + 6, 220, 220, 180, 2);
            _renderer.DrawText(item.Description, ox + 220, ry + 8, 140, 200, 140, 1);
        }
        if (_player.Inventory.Count == 0)
            DrawCentered("EMPTY", WinW / 2, oy + 100, 140, 140, 140, 2);
    }

    private void RenderGameOver()
    {
        DrawCentered("GAME OVER", WinW / 2, 200, 220, 50, 50, 4);
        DrawCentered($"SCORE: {_score}", WinW / 2, 290, 255, 215, 0, 3);
        DrawCentered($"FLOOR: {_floor}", WinW / 2, 350, 200, 200, 200, 2);
        DrawCentered("PRESS ENTER TO RETURN TO MENU", WinW / 2, 440, 160, 160, 160, 2);
    }

    private void RenderVictory()
    {
        DrawCentered("VICTORY!", WinW / 2, 180, 80, 255, 80, 4);
        DrawCentered("THE DUNGEON BOSS IS DEFEATED", WinW / 2, 270, 220, 220, 100, 2);
        DrawCentered($"SCORE: {_score}", WinW / 2, 320, 255, 215, 0, 3);
        DrawCentered($"LEVEL: {_player.Level}", WinW / 2, 390, 200, 220, 200, 2);
        DrawCentered("PRESS ENTER TO RETURN TO MENU", WinW / 2, 460, 160, 160, 160, 2);
    }

    private void DrawCentered(string text, int cx, int y, byte r, byte g, byte b, int scale = 2)
    {
        int x = cx - _renderer.TextWidth(text, scale) / 2;
        _renderer.DrawText(text, x, y, r, g, b, scale);
    }

    private static void TileColor(TileType t, out byte r, out byte g, out byte b)
    {
        switch (t)
        {
            case TileType.Floor:      r = 55;  g = 50;  b = 45;  break;
            case TileType.StairsDown: r = 80;  g = 160; b = 80;  break;
            case TileType.StairsUp:   r = 80;  g = 80;  b = 160; break;
            default:                  r = 25;  g = 22;  b = 20;  break;
        }
    }

    private static void EnemyColor(EnemyKind k, out byte r, out byte g, out byte b)
    {
        switch (k)
        {
            case EnemyKind.Goblin: r = 100; g = 200; b = 80;  break;
            case EnemyKind.Troll:  r = 160; g = 80;  b = 40;  break;
            default:               r = 220; g = 40;  b = 200; break;
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderer.Dispose();
        _scores.Dispose();
        unsafe
        {
            _sdl.DestroyRenderer((Renderer*)_rendererHandle);
            _sdl.DestroyWindow((Window*)_window);
        }
        _sdl.Quit();
        GC.SuppressFinalize(this);
    }
}
