using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.ProductTypes;
using ShoppingApi.DTOs.Products;

namespace ShoppingApi.Services.Products;

public class ProductCatalogService : IProductCatalogService
{
    private const decimal MaximumProductPrice = 9_999_999_999_999_999.99m;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public ProductCatalogService(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<IEnumerable<ProductTypeDto>> GetProductTypesAsync()
    {
        return await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .OrderBy(x => x.Name)
            .Select(x => new ProductTypeDto(
                x.Id,
                x.Name,
                x.CreatedAt,
                x.UpdatedAt,
                x.DeletedAt))
            .ToListAsync();
    }

    public async Task<ProductTypeDto?> GetProductTypeAsync(int id)
    {
        return await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new ProductTypeDto(
                x.Id,
                x.Name,
                x.CreatedAt,
                x.UpdatedAt,
                x.DeletedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<ProductTypeDto> CreateProductTypeAsync(CreateProductTypeRequest request, string userId)
    {
        var normalizedName = NormalizeProductTypeName(request.Name);
        var exists = await _dbContext.ProductTypes
            .AnyAsync(x => x.Name == normalizedName && x.DeletedAt == null);

        if (exists)
        {
            throw new ValidationException("A product type with this name already exists.");
        }

        var entity = new ProductType
        {
            Name = normalizedName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        _dbContext.ProductTypes.Add(entity);
        await _dbContext.SaveChangesAsync();

        return new ProductTypeDto(entity.Id, entity.Name, entity.CreatedAt, entity.UpdatedAt, entity.DeletedAt);
    }

    public async Task<ProductTypeDto> UpdateProductTypeAsync(int id, UpdateProductTypeRequest request, string userId)
    {
        var entity = await _dbContext.ProductTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        if (entity is null)
        {
            throw new KeyNotFoundException("Product type was not found.");
        }

        var normalizedName = NormalizeProductTypeName(request.Name);

        var nameExists = await _dbContext.ProductTypes
            .AnyAsync(x => x.Id != id && x.Name == normalizedName && x.DeletedAt == null);

        if (nameExists)
        {
            throw new ValidationException("A product type with this name already exists.");
        }

        entity.Name = normalizedName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();

        return new ProductTypeDto(entity.Id, entity.Name, entity.CreatedAt, entity.UpdatedAt, entity.DeletedAt);
    }

    public async Task DeleteProductTypeAsync(int id, string userId)
    {
        var entity = await _dbContext.ProductTypes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        if (entity is null)
        {
            throw new KeyNotFoundException("Product type was not found.");
        }

        var hasActiveProducts = await _dbContext.Products.AnyAsync(x => x.ProductTypeId == id && x.DeletedAt == null);
        if (hasActiveProducts)
        {
            throw new InvalidOperationException("A product type cannot be deleted while active products reference it.");
        }

        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = userId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(string? search = null, int? productTypeId = null)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .Include(x => x.ProductType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        if (productTypeId.HasValue)
        {
            query = query.Where(x => x.ProductTypeId == productTypeId.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.ProductTypeId,
                x.Price,
                x.Picture,
                x.PictureContentType,
                x.PictureWidth,
                x.PictureHeight,
                x.CreatedAt,
                x.UpdatedAt,
                x.DeletedAt))
            .ToListAsync();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.ProductTypeId,
                x.Price,
                x.Picture,
                x.PictureContentType,
                x.PictureWidth,
                x.PictureHeight,
                x.CreatedAt,
                x.UpdatedAt,
                x.DeletedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, string userId)
    {
        var productName = NormalizeProductName(request.Name);

        if (request.ProductTypeId <= 0)
        {
            throw new ValidationException("A valid product type is required.");
        }

        ValidatePrice(request.Price);

        var productTypeExists = await _dbContext.ProductTypes
            .AnyAsync(x => x.Id == request.ProductTypeId && x.DeletedAt == null);

        if (!productTypeExists)
        {
            throw new ValidationException("The selected product type does not exist or is not active.");
        }

        var nameExists = await _dbContext.Products
            .AnyAsync(x => x.Name == productName && x.DeletedAt == null);

        if (nameExists)
        {
            throw new ValidationException("A product with this name already exists.");
        }

        var preparedPicture = await PreparePictureAsync(request.Picture, request.PictureContentType);

        var entity = new Product
        {
            Name = productName,
            ProductTypeId = request.ProductTypeId,
            Price = request.Price,
            Picture = preparedPicture.picture,
            PictureContentType = preparedPicture.contentType,
            PictureWidth = preparedPicture.width,
            PictureHeight = preparedPicture.height,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        _dbContext.Products.Add(entity);
        await _dbContext.SaveChangesAsync();

        return new ProductDto(
            entity.Id,
            entity.Name,
            entity.ProductTypeId,
            entity.Price,
            entity.Picture,
            entity.PictureContentType,
            entity.PictureWidth,
            entity.PictureHeight,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.DeletedAt);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, string userId)
    {
        var entity = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        if (entity is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }

        var productName = NormalizeProductName(request.Name);

        if (request.ProductTypeId <= 0)
        {
            throw new ValidationException("A valid product type is required.");
        }

        ValidatePrice(request.Price);

        var productTypeExists = await _dbContext.ProductTypes
            .AnyAsync(x => x.Id == request.ProductTypeId && x.DeletedAt == null);

        if (!productTypeExists)
        {
            throw new ValidationException("The selected product type does not exist or is not active.");
        }

        var nameExists = await _dbContext.Products
            .AnyAsync(x => x.Id != id && x.Name == productName && x.DeletedAt == null);

        if (nameExists)
        {
            throw new ValidationException("A product with this name already exists.");
        }

        var preparedPicture = await PreparePictureAsync(request.Picture, request.PictureContentType);

        entity.Name = productName;
        entity.ProductTypeId = request.ProductTypeId;
        entity.Price = request.Price;
        entity.Picture = preparedPicture.picture;
        entity.PictureContentType = preparedPicture.contentType;
        entity.PictureWidth = preparedPicture.width;
        entity.PictureHeight = preparedPicture.height;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();

        return new ProductDto(
            entity.Id,
            entity.Name,
            entity.ProductTypeId,
            entity.Price,
            entity.Picture,
            entity.PictureContentType,
            entity.PictureWidth,
            entity.PictureHeight,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.DeletedAt);
    }

    public async Task DeleteProductAsync(int id, string userId)
    {
        var entity = await _dbContext.Products
            .Include(x => x.ListProducts)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);

        if (entity is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }

        var hasActiveDependents = await _dbContext.ListProducts
            .AnyAsync(x => x.ProductId == id && x.DeletedAt == null);

        if (hasActiveDependents)
        {
            throw new InvalidOperationException("A product cannot be deleted while it is referenced by an active list item.");
        }

        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = userId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;

        await _dbContext.SaveChangesAsync();
    }

    private async Task<(byte[]? picture, string? contentType, int? width, int? height)> PreparePictureAsync(byte[]? picture, string? contentType)
    {
        if (picture is null || picture.Length == 0)
        {
            return (null, null, null, null);
        }

        var maxUploadBytes = _configuration.GetValue<int?>("Image:MaxUploadSizeBytes") ?? 5 * 1024 * 1024;
        if (maxUploadBytes <= 0)
        {
            throw new InvalidOperationException("Image:MaxUploadSizeBytes must be greater than zero.");
        }

        if (picture.Length > maxUploadBytes)
        {
            throw new ValidationException("The uploaded image exceeds the configured size limit.");
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var normalizedContentType = contentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedContentType) || !allowedTypes.Contains(normalizedContentType))
        {
            throw new ValidationException("Only JPEG, PNG, and WebP images are supported.");
        }

        var targetWidth = _configuration.GetValue<int?>("Image:Width") ?? 400;
        var targetHeight = _configuration.GetValue<int?>("Image:Height") ?? 400;
        var quality = _configuration.GetValue<int?>("Image:Quality") ?? 85;
        var maxPixels = _configuration.GetValue<long?>("Image:MaxPixels") ?? 40_000_000;
        if (targetWidth <= 0 || targetHeight <= 0 || quality is < 1 or > 100 || maxPixels <= 0)
        {
            throw new InvalidOperationException("The image-processing configuration is invalid.");
        }

        try
        {
            using var input = new MemoryStream(picture, writable: false);
            var info = await Image.IdentifyAsync(input);
            if (info is null)
            {
                throw new ValidationException("The uploaded file is not a supported image.");
            }

            if ((long)info.Width * info.Height > maxPixels)
            {
                throw new ValidationException("The uploaded image dimensions exceed the configured limit.");
            }

            input.Position = 0;
            using var image = await Image.LoadAsync(input);
            using var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(targetWidth, targetHeight)
            }));

            using var output = new MemoryStream();
            if (normalizedContentType == "image/png")
            {
                await resized.SaveAsync(output, new PngEncoder());
                return (output.ToArray(), "image/png", resized.Width, resized.Height);
            }

            await resized.SaveAsync(output, new JpegEncoder { Quality = quality });
            return (output.ToArray(), "image/jpeg", resized.Width, resized.Height);
        }
        catch (ImageFormatException)
        {
            throw new ValidationException("The uploaded file is not a valid supported image.");
        }
    }

    private static string NormalizeProductTypeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException("Product type name is required.");
            }

            var normalized = name.Trim();
            if (normalized.Length > 200)
            {
                throw new ValidationException("Product type name cannot exceed 200 characters.");
            }

            return normalized;
        }

    private static string NormalizeProductName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException("Product name is required.");
            }

            var normalized = name.Trim();
            if (normalized.Length > 200)
            {
                throw new ValidationException("Product name cannot exceed 200 characters.");
            }

            return normalized;
        }

    private static void ValidatePrice(decimal price)
        {
            if (price < 0)
            {
                throw new ValidationException("Product price cannot be negative.");
            }

            if (price > MaximumProductPrice || decimal.Round(price, 2) != price)
            {
                throw new ValidationException("Product price must fit the database's two-decimal precision.");
        }
    }
}
