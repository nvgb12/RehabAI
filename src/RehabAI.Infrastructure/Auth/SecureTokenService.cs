using System.Security.Cryptography;
using RehabAI.Application.Auth;

namespace RehabAI.Infrastructure.Auth;

public sealed class SecureTokenService : ISecureTokenService
{
    private const int TokenBytes = 32;

    public string GenerateToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
    }

    public string HashToken(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(tokenBytes);

        return Base64UrlEncode(hash);
    }

    public bool TokenHashesEqual(string hash, string expectedHash)
    {
        var hashBytes = System.Text.Encoding.UTF8.GetBytes(hash);
        var expectedHashBytes = System.Text.Encoding.UTF8.GetBytes(expectedHash);

        return hashBytes.Length == expectedHashBytes.Length &&
            CryptographicOperations.FixedTimeEquals(hashBytes, expectedHashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
