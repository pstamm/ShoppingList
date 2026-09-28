using System.Text.Json;

namespace ShoppingList.Services.Authentication;

internal static class JwtTokenReader
{
    public static DateTimeOffset? ReadExpiration(string token)
    {
        var segments = token.Split('.');
        if (segments.Length != 3)
        {
            return null;
        }

        var payload = segments[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        try
        {
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!document.RootElement.TryGetProperty("exp", out var exp) ||
                !exp.TryGetInt64(out var seconds))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
