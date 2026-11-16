using SignKz.Models;

namespace SignKz.Services;

// Main logic for users, points and the leaderboard. One instance for the whole app.
public class ProgressService
{
    public const int PracticePoints = 10;
    public const int SelfCheckPoints = 3;
    public const int QuizPoints = 5;

    private readonly IProgressStore _store;
    private readonly Dictionary<string, UserProgress> _users;
    private readonly object _lock = new object();

    // Fires after any user gets new points. The leaderboard page listens to it.
    public event Action? Changed;

    public ProgressService(IProgressStore store)
    {
        _store = store;
        _users = new Dictionary<string, UserProgress>(StringComparer.OrdinalIgnoreCase);

        foreach (UserProgress user in _store.LoadAll())
        {
            _users[user.Name] = user;
        }
    }

    // Creates a new account. The name must be free.
    public UserProgress Register(string? name, string? password, string? confirmPassword, string? avatar = null)
    {
        string cleanName = CleanName(name);
        string cleanPassword = CheckPassword(password);
        if (confirmPassword != cleanPassword)
        {
            throw new ArgumentException("The passwords do not match.");
        }

        lock (_lock)
        {
            if (_users.ContainsKey(cleanName))
            {
                throw new ArgumentException("This name is already taken. Choose another one or sign in.");
            }

            UserProgress created = new UserProgress();
            created.Name = cleanName;
            created.Avatar = Avatars.Clean(avatar);
            SetPassword(created, cleanPassword);
            _users[cleanName] = created;
            _store.SaveAll(_users.Values);
            return created.Copy();
        }
    }


    public UserProgress Login(string? name, string? password)
    {
        string cleanName = (name ?? "").Trim();
        string pass = password ?? "";
        if (cleanName.Length == 0 || pass.Length == 0)
        {
            throw new ArgumentException("Enter your name and password.");
        }

        lock (_lock)
        {
            CheckNotLocked(cleanName);

            if (!_users.ContainsKey(cleanName))
            {
                RegisterFailure(cleanName);
                throw new ArgumentException(WrongLoginText);
            }

            UserProgress user = _users[cleanName];

            if (user.PasswordHash.Length == 0)
            {
                // Old account without a password: the first sign-in sets one.
                string newPassword = CheckPassword(pass);
                SetPassword(user, newPassword);
                _store.SaveAll(_users.Values);
            }
            else if (!PasswordHasher.Verify(pass, user.PasswordHash, user.PasswordSalt))
            {
                RegisterFailure(cleanName);
                throw new ArgumentException(WrongLoginText);
            }

            _failures.Remove(user.Name);
            return user.Copy();
        }
    }

    private const string WrongLoginText = "Wrong name or password.";
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, int> _failures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lockedUntil = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

    // Must be called inside the lock.
    private void CheckNotLocked(string name)
    {
        if (_lockedUntil.TryGetValue(name, out DateTime until))
        {
            if (DateTime.UtcNow < until)
            {
                throw new ArgumentException("Too many wrong attempts. Try again in a minute.");
            }
            _lockedUntil.Remove(name);
            _failures.Remove(name);
        }
    }

    // Must be called inside the lock.
    private void RegisterFailure(string name)
    {
        int count = _failures.TryGetValue(name, out int old) ? old + 1 : 1;
        _failures[name] = count;
        if (count >= MaxFailures)
        {
            _lockedUntil[name] = DateTime.UtcNow + LockTime;
        }
    }

    private static string CleanName(string? name)
    {
        string cleanName = (name ?? "").Trim();
        if (cleanName.Length == 0)
        {
            throw new ArgumentException("Please enter your name.");
        }
        if (cleanName.Length > 20)
        {
            throw new ArgumentException("The name must be 20 characters or shorter.");
        }
        return cleanName;
    }

    private static string CheckPassword(string? password)
    {
        string value = password ?? "";
        if (value.Length < 6)
        {
            throw new ArgumentException("The password must be at least 6 characters.");
        }
        if (value.Length > 64)
        {
            throw new ArgumentException("The password must be 64 characters or shorter.");
        }
        return value;
    }

    private static void SetPassword(UserProgress user, string password)
    {
        PasswordHasher.Create(password, out string hash, out string salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
    }

    public UserProgress? Get(string name)
    {
        lock (_lock)
        {
            if (_users.ContainsKey(name)) return _users[name].Copy();
            return null;
        }
    }

    // The camera confirmed the sign.
    public void RegisterPractice(string name, string signId)
    {
        lock (_lock)
        {
            UserProgress user = GetUserOrThrow(name);
            user.Attempts++;
            user.Correct++;
            user.Points += PracticePoints;
            MarkLearned(user, signId);
            Touch(user);
        }
        Changed?.Invoke();
    }

    // The user said "I did it" for a sign that the camera cannot check. Gives fewer points.
    public void RegisterSelfCheck(string name, string signId)
    {
        lock (_lock)
        {
            UserProgress user = GetUserOrThrow(name);
            user.Points += SelfCheckPoints;
            MarkLearned(user, signId);
            Touch(user);
        }
        Changed?.Invoke();
    }

    public void RegisterQuizAnswer(string name, string signId, bool isCorrect)
    {
        lock (_lock)
        {
            UserProgress user = GetUserOrThrow(name);
            user.Attempts++;
            if (isCorrect)
            {
                user.Correct++;
                user.Points += QuizPoints;
            }
            Touch(user);
        }
        Changed?.Invoke();
    }

    public List<UserProgress> GetTop(int count)
    {
        lock (_lock)
        {
            List<UserProgress> all = new List<UserProgress>();
            foreach (UserProgress user in _users.Values)
            {
                all.Add(user.Copy());
            }

            all.Sort((a, b) => b.Points.CompareTo(a.Points));
            if (all.Count > count)
            {
                all = all.GetRange(0, count);
            }
            return all;
        }
    }

    private static void MarkLearned(UserProgress user, string signId)
    {
        if (!user.LearnedSigns.Contains(signId))
        {
            user.LearnedSigns.Add(signId);
        }
    }

    // Updates the streak and the last activity time, then saves. Must be called inside the lock.
    private void Touch(UserProgress user)
    {
        DateTime now = DateTime.UtcNow;
        user.StreakDays = StreakCalculator.Update(user.StreakDays, user.LastActive, now);
        user.LastActive = now;
        _store.SaveAll(_users.Values);
    }

    private UserProgress GetUserOrThrow(string name)
    {
        if (!_users.ContainsKey(name))
        {
            throw new InvalidOperationException("User was not found: " + name);
        }
        return _users[name];
    }
}
