using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using ShoppingApi.Controllers;
using ShoppingApi.Services.Authentication;

namespace ShoppingApi.ApiTests;

public class AuthenticationHardeningTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    public void Jwt_signing_key_is_required_and_must_be_at_least_32_bytes(string? value)
    {
        var settings = new Dictionary<string, string?>();
        if (value is not null)
        {
            settings["Jwt:SigningKey"] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        Assert.Throws<InvalidOperationException>(() => JwtConfiguration.GetSigningKey(configuration));
    }

    [Fact]
    public void Jwt_signing_key_is_read_from_configuration()
    {
        const string key = "A-long-enough-development-signing-key-value";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key })
            .Build();

        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(key), JwtConfiguration.GetSigningKey(configuration));
    }

    [Fact]
    public void Anonymous_authentication_actions_are_rate_limited()
    {
        foreach (var actionName in new[] { nameof(AuthController.Register), nameof(AuthController.Login), nameof(AuthController.Refresh) })
        {
            var action = typeof(AuthController).GetMethod(actionName);

            Assert.NotNull(action);
            Assert.NotNull(action!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(action.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
        }
    }
}
