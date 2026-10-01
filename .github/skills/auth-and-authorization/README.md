# Auth and Authorization

## Purpose
Use this skill for changes involving ASP.NET Core Identity, JWTs, user roles, sharing permissions, admin access, and browser authentication flows.

## Guardrails
- The API must enforce every permission; client-side checks are only UI convenience.
- `ConnectionStrings:DefaultConnection` and `Jwt:SigningKey` are required by the API; local bootstrap admin settings are configured via `Admin:Email` and `Admin:Password` outside source-controlled settings.
- Respect list access rules: owners and explicitly shared users can edit lists; only owners and admins manage shares or delete lists; admins have broader access.
- Refresh tokens and sign-out coordination belong to the authentication/session layer, not business logic.

## Architecture
- Identity and JWT logic live in the API.
- The Blazor app stores access and refresh tokens in `sessionStorage` and attaches access tokens via the authorization handler.

## Validation
- Re-run the relevant API tests and auth-related unit tests after changing permission or token behavior.
