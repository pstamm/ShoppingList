using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingList.Services.Authentication;

public sealed class JwtAuthenticationHandler : DelegatingHandler
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly Uri _apiBaseAddress;
    private readonly AuthSessionRefreshLock _refreshLock;

    public JwtAuthenticationHandler(
        IAuthSessionStore sessionStore,
        IConfiguration configuration,
        AuthSessionRefreshLock refreshLock)
    {
        _sessionStore = sessionStore;
        _refreshLock = refreshLock;
        var apiBaseUrl = configuration["ApiBaseUrl"];
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseAddress) || apiBaseAddress is null)
        {
            throw new InvalidOperationException("ApiBaseUrl must be configured as an absolute URL.");
        }

        _apiBaseAddress = apiBaseAddress;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsAnonymousAuthRequest(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var session = await _sessionStore.GetAsync();
        if (session is null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var expiration = JwtTokenReader.ReadExpiration(session.AccessToken);
        if (expiration is null)
        {
            await _sessionStore.ClearAsync();
            return await base.SendAsync(request, cancellationToken);
        }

        if (expiration <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            session = await TryRefreshAsync(session, cancellationToken);
            if (session is null)
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }

        using var firstAttempt = await CloneRequestAsync(request, session.AccessToken, cancellationToken);
        var response = await base.SendAsync(firstAttempt, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        session = await TryRefreshAsync(session, cancellationToken);
        if (session is null)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        using var retry = await CloneRequestAsync(request, session.AccessToken, cancellationToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task<AuthSession?> TryRefreshAsync(AuthSession session, CancellationToken cancellationToken)
    {
        await _refreshLock.Semaphore.WaitAsync(cancellationToken);
        try
        {
            var latest = await _sessionStore.GetAsync();
            if (latest is null)
            {
                return null;
            }

            if (!string.Equals(latest.AccessToken, session.AccessToken, StringComparison.Ordinal))
            {
                return latest;
            }

            using var refreshRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(_apiBaseAddress, "api/auth/refresh"))
            {
                Content = JsonContent.Create(new RefreshRequest(latest.RefreshToken))
            };

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(refreshRequest, cancellationToken);
            }
            catch (HttpRequestException)
            {
                await _sessionStore.ClearAsync();
                throw;
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    await _sessionStore.ClearAsync();
                    return null;
                }

                try
                {
                    var refreshed = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(
                        cancellationToken: cancellationToken);
                    if (refreshed is null || !JwtAuthenticationStateProvider.IsValid(refreshed))
                    {
                        await _sessionStore.ClearAsync();
                        return null;
                    }

                    var updated = JwtAuthenticationStateProvider.ToSession(refreshed);
                    await _sessionStore.SetAsync(updated);
                    return updated;
                }
                catch (JsonException)
                {
                    await _sessionStore.ClearAsync();
                    return null;
                }
            }
        }
        finally
        {
            _refreshLock.Semaphore.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return clone;
    }

    private static bool IsAnonymousAuthRequest(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        return requestUri.AbsolutePath.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
               requestUri.AbsolutePath.EndsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
               requestUri.AbsolutePath.EndsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }
}
