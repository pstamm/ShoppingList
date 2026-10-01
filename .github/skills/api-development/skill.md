---
name: api-development
description: ASP.NET Core Web API patterns, DTOs, services, controllers, and repository conventions for the ShoppingList solution.
---

# API Development

## Purpose
Use this skill when making changes in `ShoppingApi` or to server-side behavior consumed by the Blazor client.

## Guardrails
- Keep controllers thin and delegate business logic to services under `ShoppingApi/Services`.
- Expose DTOs from `ShoppingApi/DTOs`; do not return EF entities directly from controllers.
- Place domain rules, validation, and list/product behavior in the matching service rather than in controllers.
- Preserve owner/share/admin rules: owners and explicitly shared users can edit lists; only owners and admins manage shares or delete lists; admins have broader access.
- Keep API contracts aligned with the client typed clients under `ShoppingList/Services`.

## Architecture
- `ShoppingApi` is an independently deployable ASP.NET Core API.
- SQL Server is accessed only by the API through EF Core.
- `ApplicationDbContext` combines Identity with application entities and mapping config in `ShoppingApi/Data/Configurations`.
- The API does not create or migrate the schema at startup; migrations are applied separately.

## Validation
- Build and test from the repo root:
  - `dotnet build .\Shopping.sln`
  - `dotnet test .\Shopping.sln`
  - targeted project tests as needed for the changed area
