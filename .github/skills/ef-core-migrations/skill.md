---
name: ef-core-migrations
description: EF Core schema work, mappings, migrations, and SQL Server persistence rules for ShoppingList.
---

# EF Core Migrations and Data Model Changes

## Purpose
Use this skill when editing the data model, entity mappings, migrations, or persistence behavior.

## Guardrails
- Add or change entity configuration in the matching `IEntityTypeConfiguration<T>` under `ShoppingApi/Data/Configurations`.
- Update and review EF Core migrations when the model changes.
- Do not replace migration-based setup with `EnsureCreated`.
- Preserve optimistic concurrency by carrying row-version values through update/delete requests and handling conflicts as conflicts.
- Keep application dates in UTC and remain compatible with `AuditSaveChangesInterceptor`.

## Architecture
- The API uses EF Core with SQL Server.
- `ApplicationDbContext` combines Identity with application entities and applies their mappings.
- Schema changes are managed through migrations, not startup creation.

## Validation
- Validate with the affected test project or build, then verify the migration is intentionally included if schema changes are part of the task.
