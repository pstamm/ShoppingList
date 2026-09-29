namespace ShoppingList.Services.Admin;

public interface IAdminUserClient
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync();
    Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequest request);
    Task<AdminUserDto> UpdateRolesAsync(string id, UpdateUserRolesRequest request);
    Task<AdminUserDto> UpdateStatusAsync(string id, UpdateUserStatusRequest request);
}
