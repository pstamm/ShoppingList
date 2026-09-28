namespace ShoppingList.Services.Authentication;

public interface IAuthSessionStore
{
    ValueTask<AuthSession?> GetAsync();
    ValueTask SetAsync(AuthSession session);
    ValueTask ClearAsync();
}
