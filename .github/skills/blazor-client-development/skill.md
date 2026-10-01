---
name: blazor-client-development
description: Blazor WebAssembly client patterns, typed clients, and browser auth behavior for the ShoppingList UI.
---

# Blazor Client Development

## Purpose
Use this skill for changes in `ShoppingList`, page logic, authentication flows, and browser-facing integration with the API.

## Guardrails
- Keep browser code focused on UI and interaction; the browser must not contain authoritative business rules or database access.
- Use typed clients under `ShoppingList/Services` rather than constructing raw HTTP requests in pages.
- Authentication relies on access and refresh tokens stored in browser `sessionStorage` via `BrowserAuthSessionStore`.
- UI authorization checks are convenience only; the API must enforce every permission.
- Configure `ApiBaseUrl` in the client and allow the origin in the API CORS settings.

## Architecture
- `ShoppingList` is a Blazor WebAssembly client.
- The client calls the API over HTTP/JSON.
- The authentication state provider and auth-session services coordinate refresh and sign-out.

## Validation
- Check the relevant client/unit tests after changes.
- Prefer focused validation: `dotnet test .\tests\ShoppingList.UnitTests\ShoppingList.UnitTests.csproj`.
