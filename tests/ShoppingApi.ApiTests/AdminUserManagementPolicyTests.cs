using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using ShoppingApi.Controllers;
using ShoppingApi.Services.Authentication;

namespace ShoppingApi.ApiTests;

public class AdminUserManagementPolicyTests
{
    [Theory]
    [InlineData(true, true, false, true, 1, true)]
    [InlineData(true, true, true, false, 1, true)]
    [InlineData(true, true, false, true, 2, false)]
    [InlineData(true, true, true, false, 2, false)]
    [InlineData(true, false, false, false, 1, false)]
    [InlineData(false, true, false, true, 1, false)]
    [InlineData(true, true, true, true, 1, false)]
    public void Last_active_admin_policy_blocks_only_removing_the_final_active_admin(
        bool targetIsAdmin,
        bool targetIsActive,
        bool willRemainAdmin,
        bool willRemainActive,
        int activeAdminCount,
        bool expected)
    {
        var result = AdminUserManagementPolicy.WouldRemoveLastActiveAdmin(
            targetIsAdmin,
            targetIsActive,
            willRemainAdmin,
            willRemainActive,
            activeAdminCount);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void User_management_controller_requires_admin_role()
    {
        var authorization = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("Admin", authorization.Roles);
    }
}
