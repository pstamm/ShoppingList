namespace ShoppingApi.DTOs.Products;

public record ProductDto(
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

public record CreateProductRequest(
    string Name,
    int ProductTypeId,
    decimal Price,
    byte[]? Picture,
    string? PictureContentType,
    int? PictureWidth,
    int? PictureHeight);

public record UpdateProductRequest(
    string Name,
    int ProductTypeId,
    decimal Price,
    byte[]? Picture,
    string? PictureContentType,
    int? PictureWidth,
    int? PictureHeight);
