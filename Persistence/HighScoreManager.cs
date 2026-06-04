using System.Text.Json;

namespace TheAdventure;

public sealed class HighScoreManager : IDisposable
{
    private static readonly string SaveDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheAdventure");
    private static readonly string ScoresFile = Path.Combine(SaveDir, "highscores.json");

    private List<HighScoreEntry> _scores = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public void Load()
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            if (!File.Exists(ScoresFile)) return;
            string json = File.ReadAllText(ScoresFile);
            _scores = JsonSerializer.Deserialize<List<HighScoreEntry>>(json) ?? new();
        }
        catch (Exception)
        {
            _scores = new();
        }
    }

    public Task LoadAsync()
    {
        Load();
        return Task.CompletedTask;
    }

    public void AddEntry(HighScoreEntry entry)
    {
        _scores.Add(entry);
        _scores = _scores.OrderByDescending(s => s.Score).Take(10).ToList();
        Persist();
    }

    public async Task AddAsync(HighScoreEntry entry)
    {
        _scores.Add(entry);
        _scores = _scores.OrderByDescending(s => s.Score).Take(10).ToList();
        await PersistAsync();
    }

    public IReadOnlyList<HighScoreEntry> TopScores =>
        _scores.OrderByDescending(s => s.Score).ToList();

    private void Persist()
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ScoresFile, json);
        }
        catch (Exception) { }
    }

    private async Task PersistAsync()
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ScoresFile, json, _cts.Token);
        }
        catch (Exception) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
