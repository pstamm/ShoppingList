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

## Run the Backend

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

