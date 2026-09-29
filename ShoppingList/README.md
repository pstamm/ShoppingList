# ShoppingList frontend

## Authentication token storage

The WebAssembly client stores the access token and refresh token together in browser `sessionStorage`, accessed only through `BrowserAuthSessionStore`. This keeps the session scoped to the current tab and avoids persistent `localStorage`, but browser storage remains readable by JavaScript and is therefore exposed if an XSS vulnerability is present. Production deployments must use HTTPS, a restrictive Content Security Policy, and careful output/input handling. The API remains authoritative for all access decisions.

Set `ApiBaseUrl` in `wwwroot/appsettings.json` (or the environment-specific appsettings file) to the deployed API origin. The API must allow the frontend origin through its CORS policy.

## IIS deployment

Publish the standalone client with `dotnet publish ShoppingList.csproj -c Release`. Its `wwwroot/web.config` enables Blazor client-side route fallback and the WebAssembly MIME type. Install IIS URL Rewrite before deploying the client. Publish the API separately with `dotnet publish ShoppingApi.csproj -c Release`; install the matching .NET 10 Hosting Bundle and configure the API site's connection string, `Jwt__SigningKey` (at least 32 UTF-8 bytes), and `Cors__AllowedOrigins__0` through IIS/environment configuration. Never deploy a signing key in source-controlled settings. Local runs must also set `Jwt__SigningKey`, for example with a randomly generated value in the current PowerShell process.

### Database setup and migrations

The API no longer creates or changes the schema on startup. For a new database, apply migrations before starting the API:

```powershell
dotnet ef database update --project ShoppingApi\ShoppingApi.csproj --startup-project ShoppingApi\ShoppingApi.csproj
```

Generate and review a deployment script with `dotnet ef migrations script --idempotent` and apply it using the deployment process. The initial migration is for a fresh database. A database previously created by `EnsureCreated` has no EF migration history; back it up and perform a deliberate schema/data transition before applying migrations—do not run the initial migration against it as-is.
