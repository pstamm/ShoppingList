namespace ShoppingList.Services.Admin;

public record AdminUserDto(
    string Id,
    string Email,
    bool IsActive,
    List<string> Roles,
    DateTimeOffset CreatedAt);

public record CreateAdminUserRequest(string Email, string Password, List<string> Roles);

public record UpdateUserRolesRequest(List<string> Roles);

public record UpdateUserStatusRequest(bool IsActive);
