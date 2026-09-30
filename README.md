# ShoppingList

A small shopping list app that helps users track items they need to buy. It provides a simple way to add, view, update, and remove shopping items from a list, making everyday errands easier to manage.

## Technologies

### Frontend
- Built with Blazor WebAssembly on .NET 10 for a client-side app that runs in the browser.
- Uses ASP.NET Core Components and HttpClient-based services to call the backend API.
- Includes client-side authentication state management and browser session storage for JWT-based auth.
- Plain web assets such as HTML and JavaScript are used for browser interactivity and image/camera support.

### Backend
- Built with ASP.NET Core Web API on .NET 10.
- Uses Entity Framework Core with SQL Server for persistence and schema management.
- Implements ASP.NET Core Identity, JWT authentication, and role-based authorization for secure access.
- Exposes REST endpoints for shopping lists, products, users, and admin operations.
- Uses Swagger/OpenAPI for API documentation and testing.
