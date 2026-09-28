namespace ShoppingApi.DTOs.Auth;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string UserId,
    IList<string> Roles);

public record MeResponse(string Id, string Email, IList<string> Roles, bool IsActive);
