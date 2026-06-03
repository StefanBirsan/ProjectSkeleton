namespace TheAdventure;

public sealed record HighScoreEntry(string Name, int Score, int Floor, string Date);

public sealed record GameSave(
    int PlayerX, int PlayerY,
    int PlayerHp, int PlayerMaxHp,
    int PlayerAttack, int PlayerDefense,
    int PlayerLevel, int PlayerXp,
    int Score, int Floor
);
