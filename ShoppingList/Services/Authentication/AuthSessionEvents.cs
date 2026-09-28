namespace ShoppingList.Services.Authentication;

public sealed class AuthSessionEvents
{
    public event Action<AuthSession?>? SessionChanged;

    public void Publish(AuthSession? session)
    {
        SessionChanged?.Invoke(session);
    }
}
