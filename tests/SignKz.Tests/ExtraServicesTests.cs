using SignKz.Models;
using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

public class LevelCalculatorTests
{
    [Theory]
    [InlineData(0, 1, "Rookie")]
    [InlineData(49, 1, "Rookie")]
    [InlineData(50, 2, "Learner")]
    [InlineData(299, 3, "Signer")]
    [InlineData(600, 5, "Master")]
    public void Get_ReturnsCorrectLevel(int points, int number, string name)
    {
        LevelInfo level = LevelCalculator.Get(points);

        Assert.Equal(number, level.Number);
        Assert.Equal(name, level.Name);
    }

    [Fact]
    public void Get_CalculatesPercentToNextLevel()
    {
        LevelInfo level = LevelCalculator.Get(25); // between 0 and 50

        Assert.Equal(50, level.PercentToNext);
        Assert.Equal(50, level.NextLevelPoints);
    }

    [Fact]
    public void Get_TopLevelHasNoNextLevel()
    {
        Assert.Null(LevelCalculator.Get(10000).NextLevelPoints);
    }
}

public class StreakCalculatorTests
{
    private readonly DateTime _today = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Update_StartsAtOne_ForNewUser()
    {
        Assert.Equal(1, StreakCalculator.Update(0, _today, _today));
    }

    [Fact]
    public void Update_KeepsStreak_OnSameDay()
    {
        Assert.Equal(3, StreakCalculator.Update(3, _today.AddHours(-3), _today));
    }

    [Fact]
    public void Update_IncreasesStreak_OnNextDay()
    {
        Assert.Equal(4, StreakCalculator.Update(3, _today.AddDays(-1), _today));
    }

    [Fact]
    public void Update_ResetsStreak_AfterMissedDay()
    {
        Assert.Equal(1, StreakCalculator.Update(5, _today.AddDays(-2), _today));
    }
}

public class AchievementServiceTests
{
    [Fact]
    public void Get_UnlocksFirstStepsAfterOneLearnedSign()
    {
        UserProgress user = new UserProgress();
        user.LearnedSigns.Add("1");

        List<Achievement> badges = AchievementService.Get(user, TestHelpers.MakeSigns());

        Assert.True(badges.Find(b => b.Title == "First Steps")!.Unlocked);
        Assert.False(badges.Find(b => b.Title == "Century Club")!.Unlocked);
    }

    [Fact]
    public void Get_CountingProNeedsAllNumbers()
    {
        List<Sign> signs = new List<Sign>
        {
            new Sign { Id = "1", Category = "Numbers" },
            new Sign { Id = "2", Category = "Numbers" }
        };
        UserProgress user = new UserProgress();
        user.LearnedSigns.Add("1");

        Assert.False(AchievementService.Get(user, signs).Find(b => b.Title == "Counting Pro")!.Unlocked);

        user.LearnedSigns.Add("2");
        Assert.True(AchievementService.Get(user, signs).Find(b => b.Title == "Counting Pro")!.Unlocked);
    }
}

public class SignRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "signkz_" + Guid.NewGuid().ToString("N"));

    public SignRepositoryTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "media"));
        File.WriteAllText(Path.Combine(_folder, "signs.json"),
            "[{\"id\":\"1\",\"title\":\"1\",\"imageUrl\":\"javascript:alert(1)\",\"videoUrl\":\"https://example.com/v\"}," +
            "{\"id\":\"a\",\"title\":\"A\"}]");
    }

    [Fact]
    public void Constructor_RemovesUnsafeLinks_AndKeepsHttps()
    {
        SignRepository repo = new SignRepository(Path.Combine(_folder, "signs.json"), Path.Combine(_folder, "media"));

        Sign sign = repo.GetById("1")!;

        Assert.Equal("", sign.ImageUrl);
        Assert.Equal("https://example.com/v", sign.VideoUrl);
    }

    [Fact]
    public void GetAll_FindsMediaFileNamedLikeSignId()
    {
        SignRepository repo = new SignRepository(Path.Combine(_folder, "signs.json"), Path.Combine(_folder, "media"));
        File.WriteAllText(Path.Combine(_folder, "media", "a.png"), "x");

        Sign sign = repo.GetById("a")!;

        Assert.Equal("media/a.png", sign.ImageUrl);
        Assert.True(sign.HasVisual);
    }

    [Fact]
    public void Sign_WithoutPatternOrImage_IsNotQuizzable()
    {
        Sign sign = new Sign { Id = "x" };

        Assert.False(sign.HasPattern);
        Assert.False(sign.HasVisual);
    }

    [Fact]
    public void Sign_WithPatternButNoImage_IsNotQuizzable()
    {
        // The finger diagram was removed, so the user has nothing to look at without a photo.
        Sign sign = new Sign { Id = "x", Pattern = "01000" };

        Assert.True(sign.HasPattern);
        Assert.False(sign.HasVisual);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }
}
