using SignKz.Models;

namespace SignKz.Services;

public static class AchievementService
{
    public static List<Achievement> Get(UserProgress user, List<Sign> allSigns)
    {
        List<Achievement> list = new List<Achievement>();

        list.Add(Make("🌱", "First Steps", "Learn your first sign.", user.LearnedSigns.Count >= 1));

        list.Add(Make("🔢", "Counting Pro", "Learn all the numbers.",
            LearnedAllInCategory(user, allSigns, "Numbers")));

        list.Add(Make("🔤", "Alphabet Explorer", "Learn 10 letters.",
            CountLearnedInCategory(user, allSigns, "Alphabet") >= 10));

        list.Add(Make("🔥", "On Fire", "Practice 3 days in a row.", user.StreakDays >= 3));

        list.Add(Make("💯", "Century Club", "Reach 100 points.", user.Points >= 100));

        list.Add(Make("🎯", "Sharp Eye", "80% accuracy after 10 attempts.",
            user.Attempts >= 10 && user.AccuracyPercent >= 80));

        return list;
    }

    private static Achievement Make(string icon, string title, string description, bool unlocked)
    {
        Achievement a = new Achievement();
        a.Icon = icon;
        a.Title = title;
        a.Description = description;
        a.Unlocked = unlocked;
        return a;
    }

    private static int CountLearnedInCategory(UserProgress user, List<Sign> allSigns, string category)
    {
        int count = 0;
        foreach (Sign sign in allSigns)
        {
            if (sign.Category == category && user.LearnedSigns.Contains(sign.Id)) count++;
        }
        return count;
    }

    private static bool LearnedAllInCategory(UserProgress user, List<Sign> allSigns, string category)
    {
        int total = 0;
        foreach (Sign sign in allSigns)
        {
            if (sign.Category == category) total++;
        }
        return total > 0 && CountLearnedInCategory(user, allSigns, category) == total;
    }
}
