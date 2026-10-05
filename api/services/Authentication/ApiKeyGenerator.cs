using System.Security.Cryptography;
using System.Text;

namespace api.Services.Authentication;

public static class ApiKeyGenerator
{
    private const string Prefix = "ak_";

    public static string GenerateKey()
    {
        return Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    public static string Hash(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Preview(string key)
    {
        var visible = key.Length <= 12 ? key : key[..12];
        return $"{visible}…";
    }
}
