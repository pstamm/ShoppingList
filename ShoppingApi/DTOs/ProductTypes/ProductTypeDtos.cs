namespace ShoppingApi.DTOs.ProductTypes;

public record ProductTypeDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public record CreateProductTypeRequest(string Name);

public record UpdateProductTypeRequest(string Name);
