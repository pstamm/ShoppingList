# Shopping List Application

## Technical Specification

**Projects**

* `ShoppingApi` — ASP.NET Core Web API
* `ShoppingList` — Standalone Blazor WebAssembly application

**Target platform:** Windows / IIS
**Database:** Microsoft SQL Server
**ORM:** Entity Framework Core, Code First
**Authentication:** ASP.NET Core Identity + custom JWT access/refresh tokens
**API documentation:** OpenAPI / Swagger
**Application roles:** `Admin`, `User`

## Table of Contents

- [1. Project Overview](#1-project-overview)
- [2. Functional Scope](#2-functional-scope)
- [3. Architecture](#3-architecture)
- [4. Technology Stack](#4-technology-stack)
- [5. Solution Structure](#5-solution-structure)
- [6. Domain Model](#6-domain-model)
- [7. User Model](#7-user-model)
- [8. Audit Model](#8-audit-model)
- [9. Concurrency](#9-concurrency)
- [10. ProductType Entity](#10-producttype-entity)
- [11. Product Entity](#11-product-entity)
- [12. ShoppingList Entity](#12-shoppinglist-entity)
- [13. ListProduct Entity](#13-listproduct-entity)
- [14. List Sharing](#14-list-sharing)
- [15. RefreshToken Entity](#15-refreshtoken-entity)
- [16. Database Relationships](#16-database-relationships)
- [17. Soft Delete](#17-soft-delete)
- [18. EF Core Configuration](#18-ef-core-configuration)
- [19. Database Constraints](#19-database-constraints)
- [20. Authentication](#20-authentication)
- [21. Registration](#21-registration)
- [22. Roles](#22-roles)
- [23. JWT Design](#23-jwt-design)
- [24. JWT Flow](#24-jwt-flow)
- [25. Logout](#25-logout)
- [26. User Management API](#26-user-management-api)
- [27. REST API](#27-rest-api)
- [28. Authentication Endpoints](#28-authentication-endpoints)
- [29. Product Type Endpoints](#29-product-type-endpoints)
- [30. Product Endpoints](#30-product-endpoints)
- [31. Product Picture Endpoints](#31-product-picture-endpoints)
- [32. Shopping List Endpoints](#32-shopping-list-endpoints)
- [33. List Product Endpoints](#33-list-product-endpoints)
- [34. Shopping Operations](#34-shopping-operations)
- [35. Sharing Endpoints](#35-sharing-endpoints)
- [36. DTO Design](#36-dto-design)
- [37. API Status Codes](#37-api-status-codes)
- [38. Error Response Format](#38-error-response-format)
- [39. Validation Rules](#39-validation-rules)
- [40. Delete Rules](#40-delete-rules)
- [41. Blazor WebAssembly Architecture](#41-blazor-webassembly-architecture)
- [42. Authentication State](#42-authentication-state)
- [43. Token Storage](#43-token-storage)
- [44. HTTP Authorization Handler](#44-http-authorization-handler)
- [45. Blazor Pages](#45-blazor-pages)
- [46. Shopping List UI](#46-shopping-list-ui)
- [47. Shopping Workflow](#47-shopping-workflow)
- [48. Immediate Save](#48-immediate-save)
- [49. Optimistic UI](#49-optimistic-ui)
- [50. Delete Confirmation](#50-delete-confirmation)
- [51. Responsive Design](#51-responsive-design)
- [52. Product List UI](#52-product-list-ui)
- [53. List Sharing UI](#53-list-sharing-ui)
- [54. Admin UI](#54-admin-ui)
- [55. State Management](#55-state-management)
- [56. API Client Design](#56-api-client-design)
- [57. Security Requirements](#57-security-requirements)
- [58. Authorization Rules](#58-authorization-rules)
- [59. User Deactivation](#59-user-deactivation)
- [60. Audit Logging](#60-audit-logging)
- [61. Logging Levels](#61-logging-levels)
- [62. Exception Handling](#62-exception-handling)
- [63. Concurrency Strategy](#63-concurrency-strategy)
- [64. Special Concurrency Consideration for Shopping](#64-special-concurrency-consideration-for-shopping)
- [65. Database Transactions](#65-database-transactions)
- [66. OpenAPI / Swagger](#66-openapi--swagger)
- [67. Configuration](#67-configuration)
- [68. Example Configuration](#68-example-configuration)
- [69. SQL Server](#69-sql-server)
- [70. Seed Data](#70-seed-data)
- [71. Testing Strategy](#71-testing-strategy)
- [72. Unit Tests](#72-unit-tests)
- [73. API Tests](#73-api-tests)
- [74. Security Tests](#74-security-tests)
- [75. Database Tests](#75-database-tests)
- [76. Frontend Testing](#76-frontend-testing)
- [77. Definition of Done](#77-definition-of-done)
- [78. Development Conventions](#78-development-conventions)
- [79. Nullable Reference Types](#79-nullable-reference-types)
- [80. Dependency Injection](#80-dependency-injection)
- [81. Controllers](#81-controllers)
- [82. Entity Exposure](#82-entity-exposure)
- [83. Repository Pattern](#83-repository-pattern)
- [84. Soft Delete Implementation](#84-soft-delete-implementation)
- [85. Date/Time Handling](#85-date-time-handling)
- [86. API Versioning](#86-api-versioning)
- [87. CORS](#87-cors)
- [88. IIS Deployment](#88-iis-deployment)
- [89. Production HTTPS](#89-production-https)
- [90. Database Backup](#90-database-backup)
- [91. Initial Implementation Plan](#91-initial-implementation-plan)
- [92. Initial Database Model Summary](#92-initial-database-model-summary)
- [93. Key Business Rules Summary](#93-key-business-rules-summary)
- [94. Important Implementation Decisions](#94-important-implementation-decisions)
- [95. Open Questions for Future Versions](#95-open-questions-for-future-versions)
- [96. Recommended MVP Boundary](#96-recommended-mvp-boundary)

---

# 1. Project Overview

## 1.1 Purpose

The Shopping List application allows authenticated users to maintain a shared catalogue of products and product types and create personal shopping lists.

Users can:

* Register and authenticate.
* View products and product types.
* Create, edit and manage products.
* Create shopping lists.
* Add products to shopping lists.
* Specify quantities to purchase.
* Track remaining quantities while shopping.
* Add notes to individual list items.
* Share their lists with other users.
* Edit lists and list items to which they have access.

Administrators have unrestricted access to application data and can manage users.

The application consists of two independently deployable projects:

```text
ShoppingList
    │
    │ HTTPS / JSON / JWT
    ▼
ShoppingApi
    │
    │ EF Core
    ▼
SQL Server
```

---

# 2. Functional Scope

## 2.1 User functionality

Authenticated users can:

* View their own shopping lists.
* Create shopping lists.
* Rename shopping lists.
* View lists explicitly shared with them.
* Edit lists they own.
* Edit shared lists.
* Share their own lists with other users.
* Add products to lists.
* Remove products from lists.
* Change quantities to order.
* Change pending quantities while shopping.
* Add/edit notes on list products.
* View products.
* Create products.
* Edit products.
* View product types.

Products and product types are global/shared resources.

## 2.2 Administrative functionality

Administrators can:

* View all users.
* Create/manage users as appropriate.
* Enable/disable users.
* Assign roles.
* View all products.
* Create/edit/delete products subject to business rules.
* View all product types.
* Create/edit/delete product types subject to business rules.
* View all shopping lists.
* Edit all shopping lists.
* Manage list contents.
* Access lists regardless of sharing permissions.

Administrative authorization must be enforced by the API.

---

# 3. Architecture

The application uses a decoupled SPA/API architecture.

```text
┌──────────────────────────────────────┐
│          ShoppingList                │
│       Blazor WebAssembly             │
│                                      │
│  Pages / Components                  │
│  Authentication State                │
│  API Clients                         │
│  DTOs                                │
└──────────────────┬───────────────────┘
                   │
                   │ HTTPS
                   │ Authorization: Bearer <JWT>
                   ▼
┌──────────────────────────────────────┐
│          ShoppingApi                 │
│        ASP.NET Core Web API          │
│                                      │
│  Controllers                         │
│  Application Services                │
│  Authorization                       │
│  Identity                            │
│  Validation                          │
│  EF Core                             │
└──────────────────┬───────────────────┘
                   │
                   │ SQL
                   ▼
┌──────────────────────────────────────┐
│           SQL Server                 │
│                                      │
│  Application tables                  │
│  ASP.NET Identity tables             │
└──────────────────────────────────────┘
```

The Blazor application must never communicate directly with SQL Server.

All business rules and authorization rules must be enforced by `ShoppingApi`.

Client-side authorization is only a UI convenience and must not be considered a security boundary. Microsoft explicitly notes that Blazor WebAssembly client code can be modified by the user and that authorization must therefore be performed on the server.

---

# 4. Technology Stack

## 4.1 Backend

* .NET 10
* ASP.NET Core 10
* ASP.NET Core Web API
* Entity Framework Core 10
* SQL Server
* ASP.NET Core Identity
* JWT Bearer Authentication
* OpenAPI 3.1
* Swagger UI
* `System.Text.Json`
* Dependency Injection
* Built-in ASP.NET Core logging

.NET 10 provides built-in OpenAPI document generation and supports OpenAPI 3.1.

## 4.2 Frontend

* Blazor WebAssembly
* .NET 10
* C#
* Razor components
* `HttpClient`
* JWT authentication
* Responsive CSS

## 4.3 Testing

* xUnit
* ASP.NET Core integration/API testing
* `WebApplicationFactory`
* EF Core test database strategy

## 4.4 Deployment

* Windows Server
* IIS
* SQL Server

---

# 5. Solution Structure

The recommended solution structure is:

```text
Shopping.sln

/src
    /ShoppingApi
        /Controllers
        /Data
        /Domain
            /Entities
            /Enums
        /DTOs
            /Auth
            /Users
            /Products
            /ProductTypes
            /Lists
            /ListProducts
            /Sharing
        /Services
            /Authentication
            /Products
            /Lists
            /Images
            /Users
        /Authorization
        /Validators
        /Middleware
        /Migrations
        Program.cs
        appsettings.json
        appsettings.Development.json

    /ShoppingList
        /Pages
        /Components
        /Layouts
        /Services
        /Authentication
        /Models
        /Http
        /State
        /wwwroot

/tests
    /ShoppingApi.UnitTests
    /ShoppingApi.ApiTests
    /ShoppingList.UnitTests
```

The exact number of projects may be reduced initially, but the logical separation should be maintained.

---

# 6. Domain Model

The application contains the following principal entities:

```text
ApplicationUser
ProductType
Product
ShoppingList
ListProduct
ListShare
RefreshToken
```

ASP.NET Core Identity also supplies its own tables for users, roles, claims, logins, etc.

---

# 7. User Model

`ApplicationUser` derives from:

```csharp
IdentityUser
```

Additional application-specific properties may be added as required.

The application has two roles:

```text
Admin
User
```

Role-based authorization is provided by ASP.NET Core Identity. ASP.NET Core supports adding roles to Identity and exposing them through the authenticated `ClaimsPrincipal`.

## 7.1 User ownership

A shopping list has an explicit:

```text
OwnerUserId
```

Products and ProductTypes are global resources and therefore do not have an ownership restriction.

---

# 8. Audit Model

All application entities subject to auditing should contain:

```text
CreatedAt
UpdatedAt
DeletedAt
CreatedByUserId
UpdatedByUserId
DeletedByUserId
```

Dates should be stored in UTC.

Example:

```csharp
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
}
```

`DeletedAt != null` represents a soft-deleted record.

---

# 9. Concurrency

All mutable application entities should have an SQL Server `rowversion` concurrency token.

Example:

```csharp
public byte[] RowVersion { get; set; } = [];
```

SQL Server `rowversion` is specifically designed for optimistic concurrency and EF Core can use it as a concurrency token.

The API should return HTTP `409 Conflict` when an update is based on stale data.

This is particularly important for:

* Shopping lists
* List products
* Products

because two users may simultaneously modify a shared shopping list.

---

# 10. ProductType Entity

Recommended name:

```text
ProductType
```

rather than `Type`, to avoid ambiguity with the C# `System.Type` concept.

Properties:

```text
Id
Name
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
DeletedAt
DeletedByUserId
RowVersion
```

## Rules

* Name is required.
* Name must be unique among active ProductTypes.
* Names should be compared case-insensitively.
* ProductType must not be hard deleted.
* A ProductType cannot be deleted while an active Product references it.
* Deleted ProductTypes must not be available for creation/editing of Products.
* Administrators may manage ProductTypes.

---

# 11. Product Entity

Properties:

```text
Id
Name
ProductTypeId
Price
Picture
PictureContentType
PictureWidth
PictureHeight
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
DeletedAt
DeletedByUserId
RowVersion
```

## 11.1 Name

Required.

Product names must be unique among active products.

Comparison should be case-insensitive.

For example:

```text
Milk
milk
MILK
```

must be considered the same product name.

## 11.2 ProductType

Required.

Every Product must reference an active ProductType.

## 11.3 Price

The price represents the current unit price.

Recommended SQL type:

```text
decimal(18,2)
```

No currency is encoded in the database model.

The application must not assume a specific currency.

## 11.4 Picture

The image is stored in SQL Server.

Recommended properties:

```text
Picture VARBINARY(MAX)
PictureContentType NVARCHAR(...)
```

The API is responsible for processing uploaded images before persistence.

The original image must not necessarily be stored.

The image pipeline is:

```text
Upload
   ↓
Validate content
   ↓
Decode image
   ↓
Resize to configured dimensions
   ↓
Encode to configured format
   ↓
Store binary data
```

The target dimensions must be configurable.

The API must enforce a maximum upload size before image processing.

Only supported image formats should be accepted.

The frontend should display a placeholder where no picture exists.

---

# 12. ShoppingList Entity

Properties:

```text
Id
Name
OwnerUserId
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
DeletedAt
DeletedByUserId
RowVersion
```

## Rules

* Name is required.
* A list may contain zero ListProducts.
* A User may create any number of lists.
* A User owns lists they create.
* Only the owner or an Admin can delete a list.
* A list can only be deleted when it contains no active ListProducts.
* Deletion is soft deletion.
* A deleted list is not returned in normal queries.
* The owner can share the list with other users.

---

# 13. ListProduct Entity

Properties:

```text
Id
ListId
ProductId
TipicalOrder
ToOrderNow
Notes
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
DeletedAt
DeletedByUserId
RowVersion
```

## 13.1 Unique product per list

A Product cannot occur more than once in the same List.

The database must enforce:

```text
UNIQUE(ListId, ProductId)
```

This constraint is required in addition to API validation.

## 13.2 TipicalOrder

`TipicalOrder` represents the usual whole-number amount the user intends to purchase.

The requirement explicitly permits:

* positive values
* zero
* negative values

The API therefore must not reject negative values.

Use an integer SQL type. Negative values, zero, and positive values are all permitted:

```text
1
2
0
-1
```

## 13.3 ToOrderNow

`ToOrderNow` represents the whole-number amount to purchase on the current shopping trip.

Use an integer SQL type. Zero and negative values are permitted, and the application must not automatically constrain it based on `TipicalOrder`.

The application must not automatically prevent a user from setting it above, below or equal to `TipicalOrder` unless a later business rule explicitly requires such validation.

This is important because the requirements explicitly permit negative quantities.

## 13.4 Notes

Optional free-form text.

---

# 14. List Sharing

Because users can explicitly share their lists, an additional entity is required.

## 14.1 ListShare

```text
Id
ListId
UserId
CreatedAt
CreatedByUserId
DeletedAt
DeletedByUserId
RowVersion
```

The pair:

```text
ListId + UserId
```

must be unique among active shares.

## 14.2 Sharing rules

A list owner can:

* Share a list.
* Remove a share.
* View current shares.

A shared user can:

* View the list.
* Edit the list.
* Add/remove ListProducts.
* Modify quantities.
* Modify notes.

A shared user cannot:

* Delete the list.
* Share the list with another user.
* Remove the owner.
* Change ownership.

Administrators can perform all operations.

This permission model is an explicit design decision based on the intended collaborative shopping workflow.

---

# 15. RefreshToken Entity

Because the application requires JWT authentication, refresh tokens should be persisted server-side.

Recommended properties:

```text
Id
UserId
TokenHash
ExpiresAt
CreatedAt
RevokedAt
ReplacedByTokenId
CreatedByIp
RevokedByIp
```

Raw refresh tokens must not be stored in the database.

Only a secure hash should be persisted.

---

# 16. Database Relationships

The logical database relationship is:

```text
AspNetUsers
    │
    ├───────────────┐
    │               │
    ▼               ▼
ShoppingLists    ListShares
    │               │
    │               ▼
    │             Users
    │
    ▼
ListProducts
    │
    ▼
Products
    │
    ▼
ProductTypes
```

More explicitly:

```text
ApplicationUser 1 ─── * ShoppingList

ApplicationUser 1 ─── * ListShare
ShoppingList   1 ─── * ListShare

ShoppingList   1 ─── * ListProduct
Product        1 ─── * ListProduct

ProductType    1 ─── * Product
ApplicationUser 1 ─── * RefreshToken
```

---

# 17. Soft Delete

Soft deletion is mandatory for application entities.

A deleted entity remains in the database but receives:

```text
DeletedAt
DeletedByUserId
```

Normal queries must exclude deleted entities.

EF Core global query filters should be used for this purpose. EF Core explicitly supports global query filters for soft deletion, and EF Core 10 additionally supports named filters.

Administrative/reporting operations may explicitly include deleted records where required.

Soft deletion must not bypass business rules.

For example:

> Product is currently used by a ListProduct.

The API must reject deletion rather than simply marking the Product deleted.

---

# 18. EF Core Configuration

The application should use:

```text
ApplicationDbContext
```

with:

```text
SQL Server provider
ASP.NET Core Identity
Application entities
```

Entity configuration should use separate `IEntityTypeConfiguration<T>` classes.

Example:

```text
Data/
    ApplicationDbContext.cs
    Configurations/
        ProductConfiguration.cs
        ProductTypeConfiguration.cs
        ShoppingListConfiguration.cs
        ListProductConfiguration.cs
        ListShareConfiguration.cs
        RefreshTokenConfiguration.cs
```

This keeps the DbContext manageable.

---

# 19. Database Constraints

The database must enforce important invariants.

Required constraints include:

```text
Product.Name unique
ProductType.Name unique
ListProduct(ListId, ProductId) unique
ListShare(ListId, UserId) unique
Product.ProductTypeId NOT NULL
ListProduct.ListId NOT NULL
ListProduct.ProductId NOT NULL
ShoppingList.OwnerUserId NOT NULL
```

Unique name indexes should account for case-insensitive SQL Server collation.

---

# 20. Authentication

Authentication uses:

```text
ASP.NET Core Identity
+
JWT Bearer Authentication
```

ASP.NET Core Identity is responsible for:

* User accounts
* Password hashing
* User validation
* Roles
* Account status
* Security stamps
* Password changes
* User management

JWT is responsible for authenticating API requests.

The API uses `JwtBearerHandler` to validate bearer tokens. JWT validation must validate the signature, issuer, audience and expiration.

---

# 21. Registration

Registration endpoint:

```http
POST /api/auth/register
```

Request:

```json
{
  "email": "user@example.com",
  "password": "..."
}
```

The server:

1. Validates the request.
2. Checks whether the email already exists.
3. Creates the Identity user.
4. Assigns the `User` role.
5. Returns an appropriate response.

New users must never be able to register themselves as `Admin`.

Administrative elevation must be performed by an authorized administrator or initial database/bootstrap process.

---

# 22. Roles

The two roles are:

```text
Admin
User
```

The first administrator must be created through a controlled seeding/bootstrap process.

Example:

```text
Admin
    └── unlimited application access

User
    ├── own lists
    ├── explicitly shared lists
    ├── shared products/types
    └── product management
```

The API should use role-based authorization for administrator operations. ASP.NET Core supports role services through Identity's `AddRoles`.

---

# 23. JWT Design

The API issues a short-lived access token.

Recommended access-token lifetime:

```text
15–30 minutes
```

The API also issues a refresh token.

Recommended refresh-token lifetime:

```text
7–30 days
```

Exact durations should be configurable.

## JWT claims

At minimum:

```text
sub
jti
iat
exp
iss
aud
email
role
```

The token should not contain unnecessary personal or application data.

---

# 24. JWT Flow

```text
User
 │
 │ POST /api/auth/login
 ▼
ShoppingApi
 │
 ├── ASP.NET Identity validates credentials
 │
 ├── Create JWT access token
 │
 └── Create refresh token
 │
 ▼
Blazor WASM
 │
 ├── Access token
 └── Refresh token
```

Subsequent requests:

```http
Authorization: Bearer <access-token>
```

When the access token expires:

```text
Blazor
   │
   │ POST /api/auth/refresh
   ▼
ShoppingApi
   │
   ├── Validate refresh token
   ├── Rotate refresh token
   └── Issue new JWT
   ▼
Blazor
```

Refresh token rotation should be implemented.

A revoked or reused refresh token must be rejected.

Microsoft's built-in ASP.NET Core Identity token endpoints issue Identity bearer tokens rather than standard JWTs, so they should not be confused with the custom JWT requirement in this specification.

---

# 25. Logout

Endpoint:

```http
POST /api/auth/logout
```

The server revokes the current refresh token.

The browser clears its authentication state.

JWT access tokens cannot generally be "unissued" once already issued, so their short lifetime limits the remaining validity period.

---

# 26. User Management API

Administrator-only endpoints:

```http
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
PUT    /api/users/{id}/roles
PUT    /api/users/{id}/status
```

The exact operations can be narrowed during implementation.

An administrator must not be able to accidentally remove the last administrator account without an appropriate protection mechanism.

---

# 27. REST API

The API should use conventional REST semantics.

Base URL:

```text
/api
```

Responses should use DTOs rather than exposing EF Core entities directly.

---

# 28. Authentication Endpoints

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

---

# 29. Product Type Endpoints

```text
GET    /api/product-types
GET    /api/product-types/{id}
POST   /api/product-types
PUT    /api/product-types/{id}
DELETE /api/product-types/{id}
```

Normal users may manage ProductTypes unless the final security policy restricts catalogue administration to Admin users.

Because ProductTypes are shared, modifications affect all users.

---

# 30. Product Endpoints

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

Optional filtering:

```text
GET /api/products?search=milk
GET /api/products?productTypeId=...
```

The API should support server-side paging if the catalogue becomes large.

---

# 31. Product Picture Endpoints

Pictures may be returned as part of the Product DTO for small images.

For better performance, a dedicated endpoint is recommended:

```http
GET /api/products/{id}/picture
```

Response:

```text
Content-Type: image/jpeg
```

or the stored image content type.

This prevents large image payloads from being included in every product-list response.

---

# 32. Shopping List Endpoints

```text
GET    /api/lists
GET    /api/lists/{id}
POST   /api/lists
PUT    /api/lists/{id}
DELETE /api/lists/{id}
```

`GET /api/lists` should return lists that the authenticated user is authorized to see:

```text
Own lists
+
Shared lists
+
All lists for Admin
```

Deleted lists are excluded unless explicitly requested by an administrator.

---

# 33. List Product Endpoints

Recommended:

```text
GET    /api/lists/{listId}/products
POST   /api/lists/{listId}/products
PUT    /api/lists/{listId}/products/{listProductId}
DELETE /api/lists/{listId}/products/{listProductId}
```

The API must verify that the caller has permission to modify the parent list.

---

# 34. Shopping Operations

The shopping workflow should support explicit quantity operations.

For example:

```text
POST /api/lists/{listId}/products/{listProductId}/pending/increase
POST /api/lists/{listId}/products/{listProductId}/pending/decrease
```

However, the preferred first implementation is to expose a normal update endpoint and let the frontend calculate the desired new value.

The server remains authoritative.

If atomic increment/decrement operations are desired later, dedicated endpoints can be introduced.

---

# 35. Sharing Endpoints

```text
GET    /api/lists/{listId}/shares
POST   /api/lists/{listId}/shares
DELETE /api/lists/{listId}/shares/{userId}
```

Only the list owner or Admin can manage sharing.

The server must verify that the target user exists and is active.

---

# 36. DTO Design

EF Core entities must not be used directly as API contracts.

Examples:

```text
ProductDto
CreateProductRequest
UpdateProductRequest

ProductTypeDto
CreateProductTypeRequest
UpdateProductTypeRequest

ShoppingListDto
CreateShoppingListRequest
UpdateShoppingListRequest

ListProductDto
AddListProductRequest
UpdateListProductRequest

ListShareDto
ShareListRequest
```

This protects the persistence model from accidental API exposure and allows API contracts to evolve independently.

---

# 37. API Status Codes

Standard HTTP status codes should be used.

| Situation                 |                Status |
| ------------------------- | --------------------: |
| Successful GET            |                   200 |
| Successful POST           |                   201 |
| Successful update         |                   200 |
| Successful delete         |                   204 |
| Invalid request           |                   400 |
| Unauthenticated           |                   401 |
| Insufficient permission   |                   403 |
| Resource not found        |                   404 |
| Duplicate resource        |                   409 |
| Concurrency conflict      |                   409 |
| Validation failure        | 422 where appropriate |
| Unexpected server failure |                   500 |

---

# 38. Error Response Format

Errors should have a consistent structure.

Recommended:

```json
{
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "name": [
      "A product with this name already exists."
    ]
  },
  "traceId": "..."
}
```

ASP.NET Core `ProblemDetails` should be used where practical.

The API must not expose stack traces or internal exception information to clients in production.

---

# 39. Validation Rules

## Product

```text
Name required
Name unique
ProductType required
ProductType must exist
ProductType must not be deleted
Price valid decimal
Picture valid supported image
Picture size within configured maximum
```

## ProductType

```text
Name required
Name unique
```

## ShoppingList

```text
Name required
Owner required
```

## ListProduct

```text
List required
Product required
Product must exist
Product must not be deleted
List must exist
List must not be deleted
Product cannot already exist in list
Quantity may be zero
Quantity may be negative
ToOrderNow may be zero
ToOrderNow may be negative
```

## ListShare

```text
List required
User required
User must exist
User must be active
List owner/Admin only
Duplicate share prohibited
```

---

# 40. Delete Rules

## Product

A Product cannot be deleted if it is referenced by an active ListProduct.

Response:

```text
409 Conflict
```

The product may only be soft-deleted after all active references have been removed.

Historical ListProduct rows should remain available for audit purposes.

## ProductType

A ProductType cannot be deleted while active Products reference it.

## List

A List must contain zero active ListProducts before deletion.

If it contains products:

```text
409 Conflict
```

The frontend must display the reason to the user.

## ListProduct

A ListProduct can be deleted by anyone with permission to edit the list.

Deletion is soft deletion.

---

# 41. Blazor WebAssembly Architecture

`ShoppingList` is a standalone Blazor WebAssembly SPA.

It communicates exclusively with `ShoppingApi`.

Recommended logical layers:

```text
Pages
   ↓
Application/UI Services
   ↓
API Client Services
   ↓
HttpClient
   ↓
ShoppingApi
```

---

# 42. Authentication State

The frontend requires an authentication-state implementation capable of:

* Reading the current access token.
* Attaching the access token to API requests.
* Detecting expired access tokens.
* Refreshing access tokens.
* Clearing authentication state when refresh fails.
* Exposing authenticated user information and roles.

The frontend must not treat the presence of UI state as proof of authorization.

The API remains authoritative.

---

# 43. Token Storage

Token storage must be carefully designed because Blazor WebAssembly executes in the browser.

Access and refresh tokens should not be exposed unnecessarily to application code.

The implementation should use a dedicated authentication/token service and avoid arbitrary use of `localStorage`.

For production security, the chosen token-storage mechanism must be explicitly documented and evaluated against XSS risk.

---

# 44. HTTP Authorization Handler

All authenticated API requests should pass through a centralized `HttpMessageHandler`/API client mechanism.

Conceptually:

```text
HttpClient
    ↓
AuthenticationHandler
    ↓
Add Authorization header
    ↓
ShoppingApi
```

This prevents every service from having to manually attach JWT tokens.

---

# 45. Blazor Pages

Recommended pages:

```text
/Login
/Register

/
/Lists
/Lists/{id}

/Products
/Products/New
/Products/{id}/Edit

/ProductTypes
/ProductTypes/New
/ProductTypes/{id}/Edit

/Users
/Users/{id}

/Lists/{id}/Share
```

Additional pages may include:

```text
/Account
/Account/ChangePassword
/Error
/Unauthorized
/NotFound
```

---

# 46. Shopping List UI

The main list page should display:

```text
List name

Product
Type
Price
Quantity
Pending
Notes
Actions
```

A ListProduct should support:

```text
TipicalOrder
    [Edit]

ToOrderNow
    [-]  3  [+]

Notes
    [Edit]
```

The UI should visually distinguish:

```text
ToOrderNow > 0
ToOrderNow = 0
ToOrderNow < 0
```

without assuming that negative values are invalid.

---

# 47. Shopping Workflow

The intended workflow is:

```text
1. Create Product
        ↓
2. Create Shopping List
        ↓
3. Add Products
        ↓
4. Set TipicalOrder and ToOrderNow
        ↓
5. Start shopping
        ↓
6. Use + / - buttons
        ↓
7. ToOrderNow changes
        ↓
8. Continue until shopping is complete
```

`TipicalOrder` is edited using an explicit edit operation.

`ToOrderNow` is optimized for rapid interaction using `+` and `-` controls.

---

# 48. Immediate Save

All changes should be persisted immediately.

For example:

```text
User presses "+"
       ↓
Update local UI
       ↓
API request
       ↓
Database update
       ↓
Confirm success
```

The UI should provide appropriate feedback when an update fails.

The application should not wait for the user to press a global "Save" button.

---

# 49. Optimistic UI

For shopping quantity changes, optimistic UI can be used:

```text
Button click
   ↓
Update displayed value immediately
   ↓
Send API request
   ↓
Success → retain
Failure → restore previous value + show error
```

Concurrency conflicts must cause the UI to refresh the affected ListProduct and inform the user.

---

# 50. Delete Confirmation

All destructive operations require confirmation.

Examples:

```text
Delete product?
Delete list?
Remove product from list?
Remove list share?
Delete product type?
```

The dialog should clearly state the consequences.

For example:

> This list can only be deleted when it contains no products.

The API remains responsible for enforcing the rule.

---

# 51. Responsive Design

The frontend must be designed mobile-first.

The shopping workflow should be optimized for a phone screen.

The primary shopping interaction should require minimal typing.

Recommended priorities on small screens:

```text
Product
Pending quantity controls
Quantity to order
Notes
```

Secondary information such as audit information can be hidden or moved to details views.

---

# 52. Product List UI

The product catalogue should support:

* Search
* Order by product type and/or Product name
* Filter by product type and/or pending quantity > 0
* Product image
* Product name
* Current price
* Edit
* Delete

The UI should avoid loading large picture blobs for every row where possible.

Use image endpoints or thumbnails.

---

# 53. List Sharing UI

The list owner should have a sharing screen/dialog.

It should allow:

```text
Search/select user
       ↓
Share
```

Existing shares:

```text
User A     [Remove]
User B     [Remove]
```

The owner must be clearly identified.

---

# 54. Admin UI

Administrators should have an Admin section containing:

```text
Users
Products
Product Types
All Lists
```

Admin pages must be protected using role-based authorization.

The API must independently enforce the same restrictions.

---

# 55. State Management

The application should avoid introducing a large global state-management framework initially.

Use:

* Component state for local UI state.
* Scoped application services for shared application concerns.
* Authentication state for identity.
* API services as the source of server data.

Example services:

```text
AuthenticationService
ProductService
ProductTypeService
ShoppingListService
ListProductService
ListSharingService
UserService
```

A lightweight application state service may be introduced later if required.

---

# 56. API Client Design

Each resource should have a dedicated client/service.

Example:

```csharp
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(...);
    Task<ProductDto> GetAsync(int id);
    Task<ProductDto> CreateAsync(...);
    Task<ProductDto> UpdateAsync(...);
    Task DeleteAsync(int id);
}
```

The frontend must not construct arbitrary URLs throughout Razor components.

---

# 57. Security Requirements

The application must:

* Require authentication for application data.
* Require JWT authentication for API endpoints.
* Validate JWT signature.
* Validate issuer.
* Validate audience.
* Validate expiration.
* Validate token type where appropriate.
* Enforce roles server-side.
* Enforce list ownership server-side.
* Enforce list sharing server-side.
* Prevent users accessing another user's private lists.
* Prevent users modifying lists they cannot edit.
* Prevent privilege escalation.
* Never accept a UserId from the client as the authoritative identity.
* Derive current user identity from JWT claims.
* Hash passwords using ASP.NET Core Identity.
* Store refresh-token hashes rather than raw refresh tokens.
* Protect signing keys.
* Use HTTPS.
* Validate uploaded images.
* Limit upload sizes.
* Avoid returning sensitive information in errors.
* Protect Swagger appropriately in production.

JWT bearer tokens must be fully validated by the API, including signature, issuer, audience and expiry.

---

# 58. Authorization Rules

A policy-based authorization layer should be introduced for resource-specific authorization.

Conceptually:

```text
Admin
    → everything

User
    → shared ProductTypes
    → shared Products
    → own Lists
    → shared Lists
```

For lists:

```text
Owner:
    Read
    Edit
    Share
    Delete

Shared User:
    Read
    Edit
    No Share
    No Delete

Admin:
    Read
    Edit
    Share
    Delete
```

For Products:

```text
Authenticated User:
    Read
    Create
    Edit
    Delete subject to usage rule

Admin:
    Full access
```

---

# 59. User Deactivation

An administrator should be able to deactivate a user.

A deactivated user:

* Cannot authenticate.
* Cannot obtain new JWT tokens.
* Cannot refresh tokens.
* Existing refresh tokens should be revoked.

Existing list ownership remains intact.

The system should not automatically delete or transfer the user's lists.

---

# 60. Audit Logging

Two forms of auditing are recommended.

## Entity audit

Stored directly on entities:

```text
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
DeletedAt
DeletedByUserId
```

## Application logging

ASP.NET Core structured logging should record:

* Authentication failures.
* Authorization failures.
* Exceptions.
* API errors.
* Important administrative operations.
* Image-processing failures.
* Database/concurrency failures.

Passwords, JWTs, refresh tokens and other secrets must never be logged.

---

# 61. Logging Levels

Development:

```text
Debug / Information
```

Production:

```text
Information
Warning
Error
Critical
```

Sensitive request/response bodies should not be logged by default.

---

# 62. Exception Handling

A centralized exception-handling middleware should convert exceptions into consistent `ProblemDetails`.

Expected exceptions should be mapped explicitly.

Example:

```text
ValidationException
    → 400

NotFoundException
    → 404

ForbiddenException
    → 403

ConflictException
    → 409

DbUpdateConcurrencyException
    → 409
```

Unexpected exceptions:

```text
500
```

with a correlation/trace ID.

---

# 63. Concurrency Strategy

Optimistic concurrency is required.

Each mutable entity should contain a `RowVersion`.

Example:

```text
User A reads ListProduct version 10

User B updates it
        ↓
version becomes 11

User A tries to update version 10
        ↓
API detects conflict
        ↓
HTTP 409
```

The frontend should then:

1. Display a concurrency message.
2. Reload the affected resource.
3. Allow the user to continue from current data.

SQL Server `rowversion` is suitable for this purpose and is supported directly by EF Core.

---

# 64. Special Concurrency Consideration for Shopping

The `ToOrderNow` operation is especially sensitive because two users may be shopping simultaneously.

Example:

```text
Pending = 5

User A presses -
User B presses -
```

The application must not accidentally turn this into:

```text
5 → 4
5 → 4
```

when the intended result is:

```text
5 → 4 → 3
```

Using `rowversion` with normal updates prevents silent lost updates.

For even more robust behavior, atomic increment/decrement API operations can later be introduced.

---

# 65. Database Transactions

A single logical update should generally be performed within a database transaction when multiple rows are affected.

Examples:

* Creating a List and initial ListProducts.
* Sharing a List.
* Revoking a refresh token and issuing a replacement.
* Administrative user changes affecting multiple Identity records.

Simple single-row CRUD operations do not require manually created transactions because EF Core already wraps `SaveChanges` appropriately.

---

# 66. OpenAPI / Swagger

The API must expose an OpenAPI document.

Development should expose Swagger UI.

The OpenAPI specification must document:

* Endpoints
* Request models
* Response models
* Authentication
* Authorization requirements
* Validation errors
* HTTP status codes

JWT bearer authentication should be represented in Swagger so developers can authenticate directly from Swagger UI.

ASP.NET Core 10 provides built-in OpenAPI generation; an interactive Swagger UI can be layered on top.

---

# 67. Configuration

Configuration should be environment-specific.

Example:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

Sensitive values must not be committed to source control.

Development secrets should use the .NET Secret Manager where appropriate. Microsoft recommends Secret Manager for local development secrets.

---

# 68. Example Configuration

Conceptually:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },

  "Jwt": {
    "Issuer": "...",
    "Audience": "...",
    "AccessTokenMinutes": 20,
    "RefreshTokenDays": 14
  },

  "Image": {
    "MaxUploadSizeBytes": 5242880,
    "Width": 400,
    "Height": 400,
    "Format": "jpeg",
    "Quality": 85
  }
}
```

JWT signing secrets must not be committed to source control.

---

# 69. SQL Server

Development uses a local SQL Server instance.

The connection string should be configurable.

EF Core migrations are the authoritative mechanism for creating/updating the schema.

Development workflow:

```text
Modify entity/configuration
        ↓
Create migration
        ↓
Review migration
        ↓
Apply migration
```

Example:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

# 70. Seed Data

The application should seed:

```text
Roles:
    Admin
    User
```

Optionally:

```text
Initial administrator
```

The initial administrator credentials must come from secure configuration/environment setup and must not be hard-coded.

No sample user should be created in production.

Optional development ProductTypes may be seeded.

---

# 71. Testing Strategy

Testing is a mandatory part of the project.

Two explicit categories are required:

```text
Unit tests
API tests
```

---

# 72. Unit Tests

Unit tests should cover:

### Business rules

* Product name uniqueness logic.
* ProductType name uniqueness logic.
* List deletion rules.
* Product deletion rules.
* Duplicate ListProduct detection.
* Sharing rules.
* Quantity handling.
* Authorization rules.
* Image validation.
* Image resizing.
* JWT/token service logic.

Services should be designed so that business logic can be tested without requiring a real HTTP server.

---

# 73. API Tests

API tests should exercise the complete HTTP pipeline.

Examples:

```text
Registration
Login
Refresh token
Logout

GET products
POST product
PUT product
DELETE product

Create list
Get list
Update list
Delete empty list
Reject deletion of non-empty list

Add product to list
Reject duplicate product
Update quantities
Delete list product

Share list
Access shared list
Reject access to private list
Remove share

Admin access
Non-admin rejection
```

API tests should verify both:

```text
HTTP status
+
response payload
```

---

# 74. Security Tests

Security/API tests should verify:

```text
Anonymous user → 401
Normal user → Admin endpoint = 403
User A → User B private list = 403/404
Shared user → shared list = allowed
Shared user → delete list = forbidden
Non-owner → share list = forbidden
Invalid JWT → 401
Expired JWT → 401
Invalid refresh token → 401
Revoked refresh token → 401
```

The exact use of `403` vs `404` for inaccessible resources should be standardized to avoid leaking resource existence where appropriate.

---

# 75. Database Tests

Database behavior should be tested against SQL Server-compatible behavior.

Important tests include:

* Unique indexes.
* Foreign keys.
* Soft deletion.
* Concurrency.
* RowVersion.
* Cascade/restrict delete behavior.

Tests should not rely exclusively on EF Core InMemory because it does not faithfully reproduce SQL Server behavior.

---

# 76. Frontend Testing

Blazor component/unit tests should cover:

* Authentication state.
* Protected navigation.
* Product forms.
* List forms.
* Quantity controls.
* Delete confirmation dialogs.
* Error display.
* Loading states.
* Unauthorized states.
* Responsive behavior where practical.

---

# 77. Definition of Done

A feature is considered complete when:

1. Domain/business rules are implemented.
2. Database model is implemented.
3. API endpoint exists.
4. API validation exists.
5. Authorization exists.
6. Unit tests exist.
7. API tests exist where applicable.
8. UI exists.
9. UI handles loading/error states.
10. Delete confirmation exists where applicable.
11. OpenAPI documentation exists.
12. Logging is implemented.
13. Concurrency behavior is handled.
14. EF migration is created.
15. The application builds successfully.

---

# 78. Development Conventions

## Naming

Use standard .NET naming:

```text
PascalCase
```

for:

* Classes
* Methods
* Properties
* Interfaces

Use:

```text
IProductService
```

for interfaces.

Use asynchronous APIs:

```text
GetProductsAsync()
```

rather than synchronous database operations.

---

# 79. Nullable Reference Types

Nullable reference types must be enabled.

```xml
<Nullable>enable</Nullable>
```

The codebase should avoid unnecessary null suppression.

---

# 80. Dependency Injection

Services should be registered using ASP.NET Core dependency injection.

Avoid static service classes.

Avoid direct construction of services inside controllers.

---

# 81. Controllers

Controllers should remain thin.

Preferred flow:

```text
Controller
    ↓
Application Service
    ↓
DbContext / repository/data access
```

Controllers should handle:

* HTTP concerns
* authentication context
* model binding
* response codes

Business logic should reside in services/domain logic.

---

# 82. Entity Exposure

Never return EF Core entities directly from API controllers.

Use DTOs.

This prevents:

* accidental sensitive data exposure
* circular serialization
* over-posting
* tight coupling
* accidental database model exposure

---

# 83. Repository Pattern

A generic repository layer is not required.

EF Core's `DbContext` and `DbSet` already provide repository/unit-of-work behavior.

Application services should use the DbContext directly unless a specific abstraction provides clear value.

---

# 84. Soft Delete Implementation

Deletion should be represented as:

```text
DeletedAt = UtcNow
DeletedByUserId = CurrentUserId
```

rather than:

```text
context.Remove(entity)
```

The application should centralize this behavior where practical.

Global EF query filters should exclude soft-deleted entities.

---

# 85. Date/Time Handling

All server-side timestamps should be UTC.

Recommended type:

```csharp
DateTimeOffset
```

The frontend converts timestamps to local display time where appropriate.

---

# 86. API Versioning

Initial version:

```text
/api/...
```

If future breaking changes are expected, introduce:

```text
/api/v1/...
```

before the first production release.

For a new application, explicit versioning from the beginning is recommended if the API may eventually have independent consumers.

---

# 87. CORS

Because the Blazor application is standalone, the API will likely be hosted on a different origin during development.

CORS must therefore be configured.

Development:

```text
localhost:xxxx
```

Production:

```text
approved ShoppingList origin
```

Do not use:

```text
AllowAnyOrigin()
```

with authenticated APIs in production.

---

# 88. IIS Deployment

The backend will be deployed as an ASP.NET Core application to IIS.

Required IIS configuration includes:

* ASP.NET Core Hosting Bundle.
* Application Pool configuration.
* HTTPS certificate.
* Application deployment directory.
* Web.config generated/configured by ASP.NET Core publish.
* Connection string configuration.
* JWT configuration.

The Blazor WebAssembly application can be deployed as static files under IIS.

Possible deployment:

```text
IIS
├── ShoppingList
│   └── Blazor WASM static files
│
└── ShoppingApi
    └── ASP.NET Core application
```

---

# 89. Production HTTPS

Production must use HTTPS.

JWTs must never be sent over unencrypted HTTP.

HTTP should redirect to HTTPS where appropriate.

---

# 90. Database Backup

SQL Server backup procedures should be established separately from the application.

At minimum:

```text
Full backup
Transaction log backup where applicable
Restore testing
```

The application must not assume that soft deletion replaces database backups.

---

# 91. Initial Implementation Plan

## Phase 1 — Solution setup

Create:

```text
Shopping.sln
ShoppingApi
ShoppingList
ShoppingApi.UnitTests
ShoppingApi.ApiTests
```

Configure:

* .NET 10
* Nullable reference types
* EF Core
* SQL Server
* OpenAPI
* Swagger

---

## Phase 2 — Identity

Implement:

```text
ApplicationUser
IdentityDbContext
Roles
Admin bootstrap
Registration
Login
JWT creation
Refresh tokens
Logout
```

Verify authentication through Swagger/API tests.

---

## Phase 3 — Domain model

Implement:

```text
ProductType
Product
ShoppingList
ListProduct
ListShare
```

Add EF configurations.

Add migrations.

Apply the initial database schema.

---

## Phase 4 — Product catalogue

Implement:

```text
ProductType CRUD
Product CRUD
Picture upload
Picture resizing
Product validation
Delete restrictions
```

Add unit/API tests.

---

## Phase 5 — Shopping lists

Implement:

```text
List CRUD
ListProduct CRUD
Duplicate prevention
Delete restrictions
```

Add authorization tests.

---

## Phase 6 — Sharing

Implement:

```text
Create share
List shares
Remove share
Shared-list authorization
Admin access
```

Test User A / User B scenarios thoroughly.

---

## Phase 7 — Blazor authentication

Implement:

```text
Login
Register
Logout
AuthenticationStateProvider
JWT handling
Protected routes
Role handling
```

---

## Phase 8 — Blazor product catalogue

Implement:

```text
Product list
Search
Product creation
Product editing
Product deletion
Product type management
Picture display
```

---

## Phase 9 — Blazor shopping lists

Implement:

```text
List overview
List creation
List editing
List detail
Add product
Edit TipicalOrder
Pending +/- controls
Notes
Immediate saving
```

---

## Phase 10 — Sharing UI

Implement:

```text
Share list
View shares
Remove shares
```

---

## Phase 11 — Admin UI

Implement:

```text
User management
Role management
User activation/deactivation
Global product management
Global list management
```

---

## Phase 12 — Hardening

Perform:

```text
Security testing
Concurrency testing
API testing
Unit testing
Responsive testing
Image-upload testing
Error-handling testing
IIS deployment testing
Database migration testing
```

---

# 92. Initial Database Model Summary

The resulting application database will conceptually contain:

```text
ASP.NET IDENTITY
─────────────────────────────────────
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetRoleClaims
AspNetUserLogins
AspNetUserTokens

APPLICATION
─────────────────────────────────────
ProductTypes
Products
ShoppingLists
ListProducts
ListShares
RefreshTokens
```

---

# 93. Key Business Rules Summary

| Rule                   | Requirement                      |
| ---------------------- | -------------------------------- |
| Authentication         | Required for application         |
| Roles                  | Admin, User                      |
| Products               | Shared globally                  |
| ProductTypes           | Shared globally                  |
| Lists                  | Owned by users                   |
| Sharing                | Explicit                         |
| Admin access           | Unlimited                        |
| Product name           | Required + unique                |
| Product type           | Required                         |
| Product type name      | Required + unique                |
| Product price          | Unit price, no currency          |
| Product picture        | Stored in DB                     |
| Picture processing     | Resize before storage            |
| Duplicate product/list | Not permitted                    |
| Quantity               | Zero and negative values allowed |
| Pending quantity       | Quantity still to purchase       |
| Empty list             | Allowed                          |
| Product deletion       | Prohibited while used            |
| List deletion          | Only when empty                  |
| Delete mechanism       | Soft delete                      |
| Audit                  | Required                         |
| Concurrency            | Optimistic                       |
| API                    | REST                             |
| API documentation      | OpenAPI/Swagger                  |
| Frontend               | Standalone Blazor WASM           |
| UI                     | Responsive/mobile-friendly       |
| Saving                 | Immediate                        |
| Delete UI              | Confirmation required            |
| Hosting                | Windows/IIS                      |
| Database               | SQL Server                       |
| ORM                    | EF Core Code First               |
| Tests                  | Unit + API                       |

---

# 94. Important Implementation Decisions

The following decisions are intentional additions to the original requirements:

### 94.1 `ProductType` instead of `Type`

The database/entity should use `ProductType` rather than `Type` to avoid ambiguity.

### 94.2 `ListShare`

List sharing requires an explicit join entity.

### 94.3 Refresh tokens

JWT access tokens should be short-lived and accompanied by securely persisted, rotated refresh tokens.

### 94.4 RowVersion

Optimistic concurrency is required because shared lists may be edited simultaneously.

### 94.5 DTOs

API entities must not be exposed directly.

### 94.6 Product image metadata

The database should store the image content type alongside the binary image.

### 94.7 Product deletion

The API rejects deletion if the product is actively referenced, even though the application uses soft deletion.

### 94.8 List deletion

A list cannot be deleted while active ListProducts exist.

### 94.9 Shared-list permissions

A shared user can edit the list but cannot delete or re-share it. The owner retains sharing/deletion privileges.

---

# 95. Open Questions for Future Versions

The specification intentionally leaves the following outside the first implementation unless required:

* Email verification.
* Password-reset email delivery.
* Multi-factor authentication.
* External login providers.
* Multiple currencies.
* Product price history.
* Shopping history.
* Completed shopping trips.
* Recurring shopping lists.
* Product favorites.
* Product categories beyond ProductType.
* Notifications.
* Push notifications.
* Offline shopping mode.
* Real-time synchronization using SignalR.
* Advanced audit history.
* Full-text product search.
* Image thumbnails/CDN storage.
* Multiple images per Product.

These should not be implemented as part of the initial MVP unless the requirements change.

---

# 96. Recommended MVP Boundary

The first production-capable version should contain:

```text
Authentication
    ├── Register
    ├── Login
    ├── Logout
    └── Refresh token

Users
    └── Admin management

Product Types
    └── CRUD

Products
    ├── CRUD
    └── Picture upload/resize

Lists
    ├── CRUD
    ├── Products
    ├── Quantities
    ├── Pending quantities
    └── Notes

Sharing
    ├── Share
    └── Unshare

Security
    ├── JWT
    ├── Roles
    ├── Resource authorization
    └── HTTPS

Persistence
    ├── SQL Server
    ├── EF Core
    ├── Code First migrations
    └── Soft deletion

Quality
    ├── Unit tests
    ├── API tests
    ├── OpenAPI
    ├── Logging
    └── Concurrency handling

Deployment
    └── Windows / IIS
```

This gives a complete vertical slice of the application without prematurely introducing features such as offline synchronization or real-time collaboration.
