using System.Text.Json;

namespace TheAdventure;

public sealed class HighScoreManager : IDisposable
{
    private static readonly string SaveDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheAdventure");
    private static readonly string ScoresFile = Path.Combine(SaveDir, "highscores.json");

    private List<HighScoreEntry> _scores = new();
    private bool _disposed;

    public async Task LoadAsync()
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            if (!File.Exists(ScoresFile)) return;
            string json = await File.ReadAllTextAsync(ScoresFile);
            _scores = JsonSerializer.Deserialize<List<HighScoreEntry>>(json) ?? new();
        }
        catch (Exception)
        {
            _scores = new();
        }
    }

    public async Task AddAsync(HighScoreEntry entry)
    {
        _scores.Add(entry);
        _scores = _scores.OrderByDescending(s => s.Score).Take(10).ToList();
        await PersistAsync();
    }

    public IReadOnlyList<HighScoreEntry> TopScores =>
        _scores.OrderByDescending(s => s.Score).ToList();

    private async Task PersistAsync()
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ScoresFile, json);
        }
        catch (Exception) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
