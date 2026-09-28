namespace ShoppingApi.DTOs.ShoppingLists;

public record ShoppingListDto(
    int Id,
    string Name,
    string OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ListProductDto> Products,
    string RowVersion);

public record CreateShoppingListRequest(string Name);

public record UpdateShoppingListRequest(string Name, string RowVersion);

public record ListProductDto(
    int Id,
    int ListId,
    int ProductId,
    string ProductName,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record AddListProductRequest(
    int ProductId,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes);

public record UpdateListProductRequest(
    int ProductId,
    decimal QuantityToOrder,
    decimal PendingQuantity,
    string? Notes,
    string RowVersion);

public record ListShareDto(
    int Id,
    int ListId,
    string UserId,
    string Email,
    DateTimeOffset CreatedAt);

public record ShareListRequest(string UserId);
