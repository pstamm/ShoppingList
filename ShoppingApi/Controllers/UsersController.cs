using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.Admin;
using ShoppingApi.Services.Authentication;

namespace ShoppingApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Admin", "User"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAll()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync();
        var results = new List<AdminUserDto>(users.Count);

        foreach (var user in users)
        {
            results.Add(await ToDtoAsync(user));
        }

        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDto>> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user is null ? NotFound() : Ok(await ToDtoAsync(user));
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateAdminUserRequest request)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var roles = NormalizeRoles(request.Roles);
        if (roles.Error is not null)
        {
            return BadRequest(new { message = roles.Error });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(error => error.Description)) });
        }

        var roleResult = await _userManager.AddToRolesAsync(user, roles.Roles!);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(new { message = string.Join(" ", roleResult.Errors.Select(error => error.Description)) });
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await ToDtoAsync(user));
    }

    [HttpPut("{id}/roles")]
    public async Task<ActionResult<AdminUserDto>> UpdateRoles(string id, [FromBody] UpdateUserRolesRequest request)
    {
        var normalizedRoles = NormalizeRoles(request.Roles);
        if (normalizedRoles.Error is not null)
        {
            return BadRequest(new { message = normalizedRoles.Error });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetIsAdmin = currentRoles.Contains("Admin", StringComparer.Ordinal);
        var willBeAdmin = normalizedRoles.Roles!.Contains("Admin", StringComparer.Ordinal);
        if (await WouldRemoveLastActiveAdmin(user, targetIsAdmin, willBeAdmin))
        {
            return Conflict(new { message = "The last active administrator cannot be demoted." });
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return IdentityFailure(removeResult);
        }

        var addResult = await _userManager.AddToRolesAsync(user, normalizedRoles.Roles!);
        if (!addResult.Succeeded)
        {
            return IdentityFailure(addResult);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return IdentityFailure(updateResult);
        }

        await RevokeRefreshTokensAsync(user.Id);
        await transaction.CommitAsync();
        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<AdminUserDto>> UpdateStatus(string id, [FromBody] UpdateUserStatusRequest request)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.IsActive == request.IsActive)
        {
            return Ok(await ToDtoAsync(user));
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (await WouldRemoveLastActiveAdmin(
                user,
                roles.Contains("Admin", StringComparer.Ordinal),
                roles.Contains("Admin", StringComparer.Ordinal),
                willRemainActive: request.IsActive))
        {
            return Conflict(new { message = "The last active administrator cannot be deactivated." });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return IdentityFailure(updateResult);
        }

        if (!request.IsActive)
        {
            await RevokeRefreshTokensAsync(user.Id);
        }

        await transaction.CommitAsync();
        return Ok(await ToDtoAsync(user));
    }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AdminUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.IsActive,
            roles,
            user.CreatedAt);
    }

    private async Task<bool> WouldRemoveLastActiveAdmin(
        ApplicationUser target,
        bool targetIsAdmin,
        bool willRemainAdmin,
        bool? willRemainActive = null)
    {
        var activeAdminCount = (await _userManager.GetUsersInRoleAsync("Admin"))
            .Count(user => user.IsActive);
        return AdminUserManagementPolicy.WouldRemoveLastActiveAdmin(
            targetIsAdmin,
            target.IsActive,
            willRemainAdmin,
            willRemainActive ?? target.IsActive,
            activeAdminCount);
    }

    private async Task RevokeRefreshTokensAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.RevokedByIp, ipAddress));
    }

    private static (IList<string>? Roles, string? Error) NormalizeRoles(IList<string>? roles)
    {
        if (roles is null || roles.Count == 0)
        {
            return (null, "At least one role must be assigned.");
        }

        var normalized = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Count == 0 || normalized.Any(role => !AllowedRoles.Contains(role, StringComparer.Ordinal)))
        {
            return (null, "Only the Admin and User roles can be assigned.");
        }

        return (normalized, null);
    }

    private BadRequestObjectResult IdentityFailure(IdentityResult result)
    {
        return BadRequest(new
        {
            message = string.Join(" ", result.Errors.Select(error => error.Description))
        });
    }
}
