# ShoppingList frontend

## Authentication token storage

The WebAssembly client stores the access token and refresh token together in browser `sessionStorage`, accessed only through `BrowserAuthSessionStore`. This keeps the session scoped to the current tab and avoids persistent `localStorage`, but browser storage remains readable by JavaScript and is therefore exposed if an XSS vulnerability is present. Production deployments must use HTTPS, a restrictive Content Security Policy, and careful output/input handling. The API remains authoritative for all access decisions.

Set `ApiBaseUrl` in `wwwroot/appsettings.json` (or the environment-specific appsettings file) to the deployed API origin. The API must allow the frontend origin through its CORS policy.
