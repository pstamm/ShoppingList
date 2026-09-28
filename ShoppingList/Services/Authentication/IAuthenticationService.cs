namespace ShoppingList.Services.Authentication;

public interface IAuthenticationService
{
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string email, string password);
    Task LogoutAsync();
}
