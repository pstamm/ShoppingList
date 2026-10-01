# Repository instructions

## Build, test, and format

Run these commands from the repository root:

```powershell
dotnet build .\Shopping.sln
dotnet test .\Shopping.sln
dotnet test .\tests\ShoppingApi.UnitTests\ShoppingApi.UnitTests.csproj
dotnet test .\tests\ShoppingApi.ApiTests\ShoppingApi.ApiTests.csproj
dotnet test .\tests\ShoppingList.UnitTests\ShoppingList.UnitTests.csproj
```

Run one test by filtering on its fully qualified name:

```powershell
dotnet test .\tests\ShoppingApi.UnitTests\ShoppingApi.UnitTests.csproj --filter "FullyQualifiedName~Namespace.ClassName.TestName"
```

Check formatting with `dotnet format .\Shopping.sln --verify-no-changes`. There is no separate repository lint command.

## Architecture

`ShoppingList` is a standalone Blazor WebAssembly client; `ShoppingApi` is an independently deployable ASP.NET Core API; SQL Server is accessed only by the API through EF Core. The browser calls the API over HTTP/JSON, and must not contain database access or authoritative business/authorization rules.

The API is organized around controllers, DTOs, domain entities, and application services. Controllers expose REST endpoints and translate service outcomes into HTTP responses; domain behavior and list/product rules belong in services. `ApplicationDbContext` combines ASP.NET Core Identity with the application entities, and applies their mappings from `Data/Configurations`. Schema changes are EF Core migrations. The API does not create or migrate the schema at startup; apply migrations before starting it.

The Blazor pages use domain-specific clients under `ShoppingList/Services` to call the API. Authentication uses access and refresh tokens in browser `sessionStorage` through `BrowserAuthSessionStore`; the authorization handler attaches access tokens, while the authentication state provider and auth-session services coordinate refresh and sign-out. Client-side authorization only controls UI behavior; the API must enforce every permission.

## Repository-specific conventions

- Keep API contracts in DTOs under `ShoppingApi/DTOs`; do not expose EF entities from controllers. Keep controllers thin and place business rules in the matching service under `ShoppingApi/Services`.
- Add or change EF mappings in the corresponding `IEntityTypeConfiguration<T>` under `ShoppingApi/Data/Configurations`. Update and review migrations when the model changes; do not replace migration-based setup with `EnsureCreated`.
- Preserve list access rules in the API: owners and explicitly shared users can edit lists; only owners and admins manage shares or delete lists; admins have broader access. Treat UI checks as convenience only.
- Carry row-version values through update/delete requests where the API uses optimistic concurrency, and handle conflicts as conflicts rather than overwriting concurrent changes.
- Audit records are captured by `AuditSaveChangesInterceptor`; keep persistence changes compatible with that interceptor and use UTC timestamps for application dates.
- Keep client HTTP calls in the matching typed client and its request/response contracts under `ShoppingList/Services`. Pages should use these clients rather than constructing endpoint requests directly.
- The API requires `ConnectionStrings:DefaultConnection` and `Jwt:SigningKey`; local bootstrap admin settings use `Admin:Email` and `Admin:Password`. Configure secrets outside source-controlled settings. Configure `ApiBaseUrl` in the client and allow its origin in the API CORS settings.
