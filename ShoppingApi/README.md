# API settings

## JWT Signing Key and initial Admin user

The JWT signing key and admin credentials are set using environment variable. 

- Jwt__SigningKey = "<random secret at least 32 UTF-8 bytes>"
- Admin__Email = "admin@example.com"
- Admin__Password = "<strong password>"

## Run the API

PowerShell example:

```
$env:Admin__Email = "admin@example.com"
$env:Admin__Password = "Abc123*$#"
$env:Jwt__SigningKey = "g1dymnXBVNjCDR98OOjnQuytsT5MSUXH"
dotnet run --project ShoppingApi\ShoppingApi.csproj
```

## Run the Frontend

```
cd ...\ShoppingList
dotnet run --project .\ShoppingList\ShoppingList.csproj
```

The frontend opens at  https://localhost:7271  (or  http://localhost:5020 ). It expects the API at  https://localhost:7271/ or http://localhost:5216 per ShoppingList\wwwroot\appsettings.json; make sure the backend is running at the configured API URL too.

## OpenAPI JSON and Swagger

### OpenAPI JSON

In development get from:

```
http://localhost:5216/openapi/v1.json
```

### Swagger UI

In development get from:

```
http://localhost:5216/swagger/v1/swagger.json
```

UI at:

```
http://localhost:5216/swagger
```

# API Deploy

Here’s a deployment guide for an IIS site named ShoppingApi. I won’t make changes or deploy it.

1. Prepare IIS: Install the IIS role and the matching .NET 10 Hosting Bundle on the machine. Create an application pool for the API with .NET CLR Version: No Managed Code.
2. Apply database migrations before starting the API. From the repository root, run:
dotnet ef database update --project .\ShoppingApi\ShoppingApi.csproj --startup-project .\ShoppingApi\ShoppingApi.csproj
``` The API does not create or update its database schema on startup. Back up the database first, and don’t run the initial migration against a database created with `EnsureCreated` without planning a schema/data transition.```
3. Publish the API to a deployment folder:
dotnet publish .\ShoppingApi\ShoppingApi.csproj -c Release -o C:\inetpub\ShoppingApi
4. Create the IIS site named  ShoppingApi , pointing its physical path to  C:\inetpub\ShoppingApi  and assigning the application pool from step 1. Add an HTTPS binding with a certificate if available; the API redirects HTTP to HTTPS. Grant the app-pool identity read/execute access to the published folder.
5. Configure runtime settings for the site or its app pool:  ConnectionStrings__DefaultConnection ,  Jwt__SigningKey  (at least 32 UTF-8 bytes), and  Cors__AllowedOrigins__0  set to the exact frontend origin. Configure  Admin__Email  and  Admin__Password  if the initial administrator bootstrap needs them. Keep secrets out of source-controlled settings; IIS process environment settings must be configured for the app pool and applied by recycling it.
6. Check startup by browsing to the site and reviewing IIS logs and Windows Event Viewer if it fails. Swagger is enabled only in Development, so it won’t normally be available when the IIS app runs as Production. Ensure the Blazor client’s  ApiBaseUrl  points to this API origin.