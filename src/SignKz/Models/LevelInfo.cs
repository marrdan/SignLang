namespace SignKz.Models;

public class LevelInfo
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public int MinPoints { get; set; }

    // null when this is the last level
    public int? NextLevelPoints { get; set; }
    public int PercentToNext { get; set; }
}
