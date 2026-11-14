namespace SignKz.Models;

public static class Avatars
{
    public static readonly string[] All = { "🦊", "🐼", "🦁", "🐯", "🐨", "🐸", "🦉", "🐙" };

    public const string Default = "🦊";

    public static string Clean(string? avatar)
    {
        if (avatar != null && Array.IndexOf(All, avatar) >= 0)
        {
            return avatar;
        }
        return Default;
    }
}
