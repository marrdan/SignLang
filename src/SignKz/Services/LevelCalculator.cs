using SignKz.Models;

namespace SignKz.Services;

public static class LevelCalculator
{
    private static readonly string[] Names = { "Rookie", "Learner", "Signer", "Talker", "Master" };
    private static readonly int[] MinPoints = { 0, 50, 150, 300, 600 };

    public static LevelInfo Get(int points)
    {
        int index = 0;
        for (int i = 0; i < MinPoints.Length; i++)
        {
            if (points >= MinPoints[i]) index = i;
        }

        LevelInfo info = new LevelInfo();
        info.Number = index + 1;
        info.Name = Names[index];
        info.MinPoints = MinPoints[index];

        if (index < MinPoints.Length - 1)
        {
            int next = MinPoints[index + 1];
            info.NextLevelPoints = next;
            info.PercentToNext = (points - info.MinPoints) * 100 / (next - info.MinPoints);
        }
        else
        {
            info.NextLevelPoints = null;
            info.PercentToNext = 100;
        }
        return info;
    }
}
