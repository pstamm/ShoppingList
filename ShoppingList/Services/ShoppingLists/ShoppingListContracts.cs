namespace ShoppingList.Services.ShoppingLists;

public sealed record ShoppingListDto(
    int Id,
    string Name,
    string OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ListProductDto> Products,
    string RowVersion);

public sealed record CreateShoppingListRequest(string Name);

public sealed record UpdateShoppingListRequest(string Name, string RowVersion);

public sealed record ListProductDto(
    int Id,
    int ListId,
    int ProductId,
    string ProductName,
    int ProductTypeId,
    string ProductTypeName,
    decimal ProductPrice,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public sealed record AddListProductRequest(
    int ProductId,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes);

public sealed record UpdateListProductRequest(
    int ProductId,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes,
    string RowVersion);

public sealed record ListShareDto(
    int Id,
    int ListId,
    string UserId,
    string Email,
    DateTimeOffset CreatedAt);

public sealed record ShareListRequest(string UserId);
