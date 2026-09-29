using Microsoft.Extensions.Configuration;
using ShoppingApi.Services.Authentication;

namespace ShoppingApi.UnitTests;

public class JwtConfigurationTests
{
    [Fact]
    public void Missing_production_signing_key_fails_fast()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => JwtConfiguration.GetSigningKey(configuration));

        Assert.Contains("must be configured", error.Message);
    }

    [Fact]
    public void Signing_key_must_have_at_least_32_utf8_bytes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "too-short" })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => JwtConfiguration.GetSigningKey(configuration));

        Assert.Contains("32 UTF-8 bytes", error.Message);
    }
}
