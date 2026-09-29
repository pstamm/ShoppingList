using System.Text;

namespace ShoppingApi.Services.Authentication;

public static class JwtConfiguration
{
    public static byte[] GetSigningKey(IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured using a secret provider or environment variable.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        return keyBytes;
    }
}
