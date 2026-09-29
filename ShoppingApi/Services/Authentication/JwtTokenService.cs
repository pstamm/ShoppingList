using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Services.Authentication;

public interface IJwtTokenService
{
    string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string CreateRefreshToken();
    ClaimsPrincipal ValidateAccessToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "ShoppingApi";
        var audience = _configuration["Jwt:Audience"] ?? "ShoppingList";
        var key = new SymmetricSecurityKey(JwtConfiguration.GetSigningKey(_configuration));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(20),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal ValidateAccessToken(string token)
    {
        var key = new SymmetricSecurityKey(JwtConfiguration.GetSigningKey(_configuration));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "ShoppingApi",
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"] ?? "ShoppingList",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, validationParameters, out _);
    }
}
