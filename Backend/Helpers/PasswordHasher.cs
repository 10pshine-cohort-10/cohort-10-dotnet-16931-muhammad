using System.Security.Cryptography;
using System.Text;

namespace TaskManagement.API.Helpers;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "SALT_KEY_CHANGE_ME");
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    public static bool Verify(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        return Hash(password) == hash;
    }
}