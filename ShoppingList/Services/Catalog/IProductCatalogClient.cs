namespace ShoppingList.Services.Catalog;

public interface IProductCatalogClient
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? search, int? productTypeId);
    Task<ProductDto> GetProductAsync(int id);
    Task<ProductDto> CreateProductAsync(SaveProductRequest request);
    Task<ProductDto> UpdateProductAsync(int id, SaveProductRequest request);
    Task DeleteProductAsync(int id);

    Task<IReadOnlyList<ProductTypeDto>> GetProductTypesAsync();
    Task<ProductTypeDto> CreateProductTypeAsync(string name);
    Task<ProductTypeDto> UpdateProductTypeAsync(int id, string name);
    Task DeleteProductTypeAsync(int id);
}
