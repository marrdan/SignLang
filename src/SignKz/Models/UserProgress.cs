namespace SignKz.Models;

public class UserProgress
{
    public string Name { get; set; } = "";
    public string Avatar { get; set; } = Avatars.Default;

    // Password (PBKDF2 hash + salt, never the password itself). Empty for old accounts made before passwords existed.
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int Points { get; set; }
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public int StreakDays { get; set; }
    public List<string> LearnedSigns { get; set; } = new List<string>();
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    public int AccuracyPercent
    {
        get
        {
            if (Attempts == 0) return 0;
            return (int)Math.Round(100.0 * Correct / Attempts);
        }
    }

    // Returns a separate copy, so the UI cannot change the stored data by accident.
    // The password data is not copied: the UI never needs it.
    public UserProgress Copy()
    {
        UserProgress copy = new UserProgress();
        copy.Name = Name;
        copy.Avatar = Avatar;
        copy.Points = Points;
        copy.Attempts = Attempts;
        copy.Correct = Correct;
        copy.StreakDays = StreakDays;
        copy.LearnedSigns = new List<string>(LearnedSigns);
        copy.LastActive = LastActive;
        return copy;
    }
}
