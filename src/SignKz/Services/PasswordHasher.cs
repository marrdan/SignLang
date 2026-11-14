using System.Security.Cryptography;

namespace SignKz.Services;

// Passwords are never stored as text. We store a random salt and a PBKDF2 hash.
public static class PasswordHasher
{
    private const int Iterations = 100000;
    private const int HashBytes = 32;
    private const int SaltBytes = 16;

    public static void Create(string password, out string hash, out string salt)
    {
        byte[] saltData = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] hashData = Rfc2898DeriveBytes.Pbkdf2(password, saltData, Iterations, HashAlgorithmName.SHA256, HashBytes);
        hash = Convert.ToBase64String(hashData);
        salt = Convert.ToBase64String(saltData);
    }

    public static bool Verify(string password, string hash, string salt)
    {
        if (hash.Length == 0 || salt.Length == 0) return false;

        try
        {
            byte[] saltData = Convert.FromBase64String(salt);
            byte[] expected = Convert.FromBase64String(hash);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, saltData, Iterations, HashAlgorithmName.SHA256, expected.Length);
            // Compares in constant time, so the time does not reveal how many bytes were right.
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
