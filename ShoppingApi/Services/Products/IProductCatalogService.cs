using ShoppingApi.DTOs.ProductTypes;
using ShoppingApi.DTOs.Products;

namespace ShoppingApi.Services.Products;

public interface IProductCatalogService
{
    Task<IEnumerable<ProductTypeDto>> GetProductTypesAsync();
    Task<ProductTypeDto?> GetProductTypeAsync(int id);
    Task<ProductTypeDto> CreateProductTypeAsync(CreateProductTypeRequest request, string userId);
    Task<ProductTypeDto> UpdateProductTypeAsync(int id, UpdateProductTypeRequest request, string userId);
    Task DeleteProductTypeAsync(int id, string userId);

    Task<IEnumerable<ProductDto>> GetProductsAsync(string? search = null, int? productTypeId = null);
    Task<ProductDto?> GetProductAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, string userId);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, string userId);
    Task DeleteProductAsync(int id, string userId);
}
