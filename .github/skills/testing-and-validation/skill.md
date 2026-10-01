---
name: testing-and-validation
description: .NET test execution and validation workflows for builds, targeted test runs, and formatting checks in ShoppingList.
---

# Testing and Validation

## Purpose
Use this skill before finishing a change to confirm the repository standards are preserved.

## Required validation
Run these commands from the repository root as appropriate:

```powershell
dotnet build .\Shopping.sln
dotnet test .\Shopping.sln
dotnet test .\tests\ShoppingApi.UnitTests\ShoppingApi.UnitTests.csproj
dotnet test .\tests\ShoppingApi.ApiTests\ShoppingApi.ApiTests.csproj
dotnet test .\tests\ShoppingList.UnitTests\ShoppingList.UnitTests.csproj
```

For a single test, use a filter with its fully qualified name:

```powershell
dotnet test .\tests\ShoppingApi.UnitTests\ShoppingApi.UnitTests.csproj --filter "FullyQualifiedName~Namespace.ClassName.TestName"
```

## Formatting
- Check formatting with `dotnet format .\Shopping.sln --verify-no-changes`.

## Expectations
- Prefer the smallest relevant validation command for the changed area.
- If a task modifies API contracts, auth behavior, or schema changes, validate the related API and client test coverage rather than relying on a single subset.
