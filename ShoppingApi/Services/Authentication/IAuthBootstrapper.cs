namespace ShoppingApi.Services.Authentication;

public interface IAuthBootstrapper
{
    Task EnsureSeededAsync();
}
