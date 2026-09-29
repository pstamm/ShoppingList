namespace ShoppingApi.DTOs.Admin;

public record AdminUserDto(
    string Id,
    string Email,
    bool IsActive,
    IList<string> Roles,
    DateTimeOffset CreatedAt);

public record CreateAdminUserRequest(string Email, string Password, IList<string> Roles);

public record UpdateUserRolesRequest(IList<string> Roles);

public record UpdateUserStatusRequest(bool IsActive);
