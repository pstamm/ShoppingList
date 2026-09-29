using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShoppingList;
using ShoppingList.Services.Catalog;
using ShoppingList.Services.Authentication;
using ShoppingList.Services.Admin;
using ShoppingList.Services.ShoppingLists;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out _))
{
    throw new InvalidOperationException("ApiBaseUrl must be configured as an absolute URL.");
}

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<AuthSessionEvents>();
builder.Services.AddSingleton<AuthSessionRefreshLock>();
builder.Services.AddScoped<IAuthSessionStore, BrowserAuthSessionStore>();
builder.Services.AddScoped<JwtAuthenticationHandler>();
builder.Services.AddHttpClient("ShoppingApi", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<JwtAuthenticationHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ShoppingApi"));
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IProductCatalogClient, ProductCatalogClient>();
builder.Services.AddScoped<IAdminUserClient, AdminUserClient>();
builder.Services.AddScoped<IShoppingListClient, ShoppingListClient>();

await builder.Build().RunAsync();
