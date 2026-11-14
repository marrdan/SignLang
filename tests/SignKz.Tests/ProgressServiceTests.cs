using SignKz.Models;
using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

// A fake store that keeps data in memory, so the tests do not touch the disk.
public class FakeProgressStore : IProgressStore
{
    public List<UserProgress> Saved = new List<UserProgress>();

    public List<UserProgress> LoadAll()
    {
        return Saved;
    }

    public void SaveAll(IEnumerable<UserProgress> users)
    {
        Saved = new List<UserProgress>();
        foreach (UserProgress user in users)
        {
            Saved.Add(user.Copy());
        }
    }
}

public class ProgressServiceTests
{
    [Fact]
    public void Register_CreatesNewUser()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());

        UserProgress user = service.Register("  Mard ", "secret1", "secret1");

        Assert.Equal("Mard", user.Name);
        Assert.Equal(0, user.Points);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("this name is much too long for the app")]
    public void Register_Throws_ForBadName(string name)
    {
        ProgressService service = new ProgressService(new FakeProgressStore());

        Assert.Throws<ArgumentException>(() => service.Register(name, "secret1", "secret1"));
    }

    [Fact]
    public void RegisterPractice_AddsPointsAndLearnedSignOnce()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        service.RegisterPractice("Mard", "a");
        service.RegisterPractice("mard", "a");

        UserProgress user = service.Get("Mard")!;
        Assert.Equal(2 * ProgressService.PracticePoints, user.Points);
        Assert.Single(user.LearnedSigns);
    }

    [Fact]
    public void RegisterQuizAnswer_GivesPointsOnlyForCorrectAnswer()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        service.RegisterQuizAnswer("Mard", "l", true);
        service.RegisterQuizAnswer("Mard", "l", false);

        UserProgress user = service.Get("Mard")!;
        Assert.Equal(ProgressService.QuizPoints, user.Points);
        Assert.Equal(2, user.Attempts);
        Assert.Equal(1, user.Correct);
        Assert.Equal(50, user.AccuracyPercent);
    }

    [Fact]
    public void GetTop_SortsByPoints()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Aida", "secret1", "secret1");
        service.Register("Mard", "secret1", "secret1");
        service.RegisterPractice("Mard", "a");

        List<UserProgress> top = service.GetTop(10);

        Assert.Equal("Mard", top[0].Name);
    }

    [Fact]
    public void RegisterPractice_RaisesChangedEvent()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");
        bool raised = false;
        service.Changed += () => raised = true;

        service.RegisterPractice("Mard", "a");

        Assert.True(raised);
    }

    [Fact]
    public void RegisterSelfCheck_GivesFewerPoints_AndDoesNotChangeAccuracy()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        service.RegisterSelfCheck("Mard", "a");

        UserProgress user = service.Get("Mard")!;
        Assert.Equal(ProgressService.SelfCheckPoints, user.Points);
        Assert.Equal(0, user.Attempts);
        Assert.Contains("a", user.LearnedSigns);
    }

    [Fact]
    public void Register_UsesValidAvatar_AndFallsBackForInvalidOne()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());

        Assert.Equal("🐼", service.Register("Aida", "secret1", "secret1", "🐼").Avatar);
        Assert.Equal(Avatars.Default, service.Register("Mard", "secret1", "secret1", "not-an-avatar").Avatar);
    }

    [Fact]
    public void FirstActivity_StartsStreakAtOne()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        service.RegisterPractice("Mard", "1");

        Assert.Equal(1, service.Get("Mard")!.StreakDays);
    }

    // ---- passwords ----

    [Fact]
    public void Register_Throws_WhenNameIsTaken_EvenWithDifferentCase()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        Assert.Throws<ArgumentException>(() => service.Register("mard", "other-pass", "other-pass"));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("")]
    public void Register_Throws_ForShortPassword(string password)
    {
        ProgressService service = new ProgressService(new FakeProgressStore());

        Assert.Throws<ArgumentException>(() => service.Register("Mard", password, password));
    }

    [Fact]
    public void Register_Throws_WhenPasswordsDoNotMatch()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());

        Assert.Throws<ArgumentException>(() => service.Register("Mard", "secret1", "secret2"));
    }

    [Fact]
    public void Login_Works_WithCorrectPassword()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        UserProgress user = service.Login("mard", "secret1");

        Assert.Equal("Mard", user.Name);
    }

    [Fact]
    public void Login_Throws_ForWrongPassword_AndForUnknownUser_WithSameMessage()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        ArgumentException wrong = Assert.Throws<ArgumentException>(() => service.Login("Mard", "nope-nope"));
        ArgumentException unknown = Assert.Throws<ArgumentException>(() => service.Login("Ghost", "secret1"));

        Assert.Equal(wrong.Message, unknown.Message);
    }

    [Fact]
    public void Login_LocksTheName_AfterTooManyWrongPasswords()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        for (int i = 0; i < 5; i++)
        {
            Assert.Throws<ArgumentException>(() => service.Login("Mard", "wrong-pass"));
        }

        // Even the right password is refused while the name is locked.
        Assert.Throws<ArgumentException>(() => service.Login("Mard", "secret1"));
    }

    [Fact]
    public void Login_OldAccountWithoutPassword_SetsThePassword()
    {
        FakeProgressStore store = new FakeProgressStore();
        store.Saved.Add(new UserProgress { Name = "Old", Points = 20 });
        ProgressService service = new ProgressService(store);

        service.Login("Old", "new-secret");

        Assert.Equal(20, service.Login("Old", "new-secret").Points);
        Assert.Throws<ArgumentException>(() => service.Login("Old", "another-one"));
    }

    [Fact]
    public void Get_DoesNotExposePasswordData()
    {
        ProgressService service = new ProgressService(new FakeProgressStore());
        service.Register("Mard", "secret1", "secret1");

        UserProgress user = service.Get("Mard")!;

        Assert.Equal("", user.PasswordHash);
        Assert.Equal("", user.PasswordSalt);
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void Verify_AcceptsRightPassword_AndRejectsWrongOne()
    {
        PasswordHasher.Create("secret1", out string hash, out string salt);

        Assert.True(PasswordHasher.Verify("secret1", hash, salt));
        Assert.False(PasswordHasher.Verify("secret2", hash, salt));
    }

    [Fact]
    public void Create_UsesRandomSalt_SoSamePasswordGivesDifferentHash()
    {
        PasswordHasher.Create("secret1", out string hash1, out string salt1);
        PasswordHasher.Create("secret1", out string hash2, out string salt2);

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForEmptyOrBrokenData()
    {
        Assert.False(PasswordHasher.Verify("x", "", ""));
        Assert.False(PasswordHasher.Verify("x", "not base64!", "also not!"));
    }
}
