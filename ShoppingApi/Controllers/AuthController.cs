using System.Security.Claims;
using System.Security.Cryptography;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.Auth;
using ShoppingApi.Services.Authentication;

namespace ShoppingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem();
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ValidationProblem(new ValidationProblemDetails(roleResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())));
        }

        return StatusCode(StatusCodes.Status201Created, new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem();
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.CreateRefreshToken();
        await StoreRefreshTokenAsync(user, refreshToken);

        return Ok(new AuthResponse(accessToken, refreshToken, user.Email ?? string.Empty, user.Id, roles.ToList()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unauthorized();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (storedToken is null ||
            storedToken.IsRevoked ||
            storedToken.IsExpired ||
            !storedToken.User.IsActive)
        {
            return Unauthorized();
        }

        var replacementToken = _jwtTokenService.CreateRefreshToken();
        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        storedToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        storedToken.ReplacedByTokenId = HashToken(replacementToken);
        _dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(storedToken.User, replacementToken));
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var roles = await _userManager.GetRolesAsync(storedToken.User);
        var accessToken = _jwtTokenService.CreateAccessToken(storedToken.User, roles);
        return Ok(new AuthResponse(
            accessToken,
            replacementToken,
            storedToken.User.Email ?? string.Empty,
            storedToken.User.Id,
            roles.ToList()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return NoContent();
        }

        var tokenHash = HashToken(request.RefreshToken);
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(item =>
            item.UserId == userId &&
            item.TokenHash == tokenHash &&
            item.RevokedAt == null);

        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _dbContext.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new MeResponse(user.Id, user.Email ?? string.Empty, roles.ToList(), user.IsActive));
    }

    private async Task StoreRefreshTokenAsync(ApplicationUser user, string token)
    {
        _dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user, token));
        await _dbContext.SaveChangesAsync();
    }

    private RefreshToken CreateRefreshTokenEntity(ApplicationUser user, string token)
    {
        var lifetimeDays = _configuration.GetValue<int?>("Jwt:RefreshTokenLifetimeDays") ?? 14;
        if (lifetimeDays <= 0)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenLifetimeDays must be greater than zero.");
        }

        return new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(lifetimeDays),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
