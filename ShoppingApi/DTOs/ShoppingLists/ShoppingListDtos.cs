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
    int ProductTypeId,
    string ProductTypeName,
    decimal ProductPrice,
    int TipicalOrder,
    int ToOrderNow,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record AddListProductRequest(
    int ProductId,
    int TipicalOrder,
    int ToOrderNow,
    string? Notes);

public record UpdateListProductRequest(
    int ProductId,
    int TipicalOrder,
    int ToOrderNow,
    string? Notes,
    string RowVersion);

public record ListShareDto(
    int Id,
    int ListId,
    string UserId,
    string UserName,
    string Email,
    DateTimeOffset CreatedAt);

public record ShareListRequest(string UserName);
