namespace ShoppingList.Services.Authentication;

public sealed class AuthSessionRefreshLock
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
