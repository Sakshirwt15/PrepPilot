using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrepPilot.Application.Abstractions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Infrastructure.Security;

public class JwtOptions
{
    /// <summary>Signing key. Must be at least 32 characters. Override it in production via environment variables.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PrepPilot";
    public string Audience { get; set; } = "PrepPilotUsers";
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7;
}

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            return false; // malformed hash must never act like a valid password
        }
    }
}

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    public AuthToken Create(User user)
    {
        var settings = options.Value;
        if (settings.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256);

        var expires = time.GetUtcNow().UtcDateTime.AddMinutes(settings.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
