using Microsoft.AspNetCore.Identity;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Services.Authentication;

public class AuthBootstrapper : IAuthBootstrapper
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthBootstrapper(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task EnsureSeededAsync()
    {
        foreach (var roleName in new[] { "Admin", "User" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"creating role '{roleName}'");
            }
        }

        var adminEmail = _configuration["Admin:Email"];
        var adminPassword = _configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(adminUser, adminPassword);
            EnsureSucceeded(createResult, "creating the configured administrator account");
        }

        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var result = await _userManager.AddToRoleAsync(adminUser, "Admin");
            EnsureSucceeded(result, "assigning the configured administrator role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
        }
    }
}
