using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.API.Entities;
using TaskManagement.API.Interfaces;

namespace TaskManagement.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expiresMinutes;

    public TokenService(IConfiguration config)
    {
        _config = config;

        var jwt = _config.GetSection("Jwt");

        _key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        _issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var expiresRaw = jwt["ExpiresMinutes"] ?? throw new InvalidOperationException("Jwt:ExpiresMinutes is not configured.");
        if (!double.TryParse(expiresRaw, out var expiresMinutes) || expiresMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiresMinutes must be a positive number.");

        _expiresMinutes = expiresMinutes;

        // Validate the key is usable for HMAC-SHA256 (>= 256 bits).
        if (Encoding.UTF8.GetByteCount(_key) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HMAC-SHA256.");
    }

    public (string token, DateTime expiresAt) CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_expiresMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}