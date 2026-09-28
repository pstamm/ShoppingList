namespace ShoppingList.Services.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthSessionResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string UserId,
    IList<string> Roles);

public sealed record AuthSession(
    string AccessToken,
    string RefreshToken,
    string Email,
    string UserId,
    string[] Roles);
