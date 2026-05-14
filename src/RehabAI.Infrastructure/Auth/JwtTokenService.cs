using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RehabAI.Application.Auth;

namespace RehabAI.Infrastructure.Auth;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private const string DefaultIssuer = "RehabAI";
    private const string DefaultAudience = "RehabAI.Client";
    private const int DefaultAccessTokenMinutes = 60;

    public string CreateAccessToken(UserAuthenticationRecord user)
    {
        var issuer = configuration["Jwt:Issuer"] ?? DefaultIssuer;
        var audience = configuration["Jwt:Audience"] ?? DefaultAudience;
        var signingKey = configuration["Jwt:SigningKey"] ??
            "development-only-rehabai-local-signing-key-change-before-production";
        var accessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : DefaultAccessTokenMinutes;
        var now = DateTimeOffset.UtcNow;

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object?>
        {
            ["sub"] = user.UserId.ToString(),
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["roles"] = user.Roles,
            ["iss"] = issuer,
            ["aud"] = audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(accessTokenMinutes).ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = Sign(unsignedToken, signingKey);

        return $"{unsignedToken}.{signature}";
    }

    private static string Sign(string unsignedToken, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));

        return Base64UrlEncode(signatureBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
