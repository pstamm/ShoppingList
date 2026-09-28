namespace ShoppingList.Services.Catalog;

public sealed record ProductTypeDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public sealed record CreateProductTypeRequest(string Name);

public sealed record UpdateProductTypeRequest(string Name);

public sealed record ProductDto(
    int Id,
    string Name,
    int ProductTypeId,
    decimal Price,
    byte[]? Picture,
    string? PictureContentType,
    int? PictureWidth,
    int? PictureHeight,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public sealed record SaveProductRequest(
    string Name,
    int ProductTypeId,
    decimal Price,
    byte[]? Picture,
    string? PictureContentType,
    int? PictureWidth,
    int? PictureHeight);
