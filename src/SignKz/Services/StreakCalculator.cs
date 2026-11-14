namespace SignKz.Services;

// "Streak" = how many days in a row the user was active.
public static class StreakCalculator
{
    public static int Update(int currentStreak, DateTime lastActiveUtc, DateTime nowUtc)
    {
        if (currentStreak <= 0)
        {
            return 1;
        }

        int daysPassed = (nowUtc.Date - lastActiveUtc.Date).Days;

        if (daysPassed <= 0) return currentStreak;      // same day
        if (daysPassed == 1) return currentStreak + 1;  // next day
        return 1;                                       // a day was missed
    }
}
