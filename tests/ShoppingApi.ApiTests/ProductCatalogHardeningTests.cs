using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.Products;
using ShoppingApi.Services.Products;

namespace ShoppingApi.ApiTests;

public class ProductCatalogHardeningTests
{
    [Fact]
    public async Task Image_upload_is_decoded_resized_and_reencoded()
    {
        await using var db = CreateContext();
        var type = new ProductType { Name = "Food" };
        db.ProductTypes.Add(type);
        await db.SaveChangesAsync();
        var service = new ProductCatalogService(db, CreateConfiguration());

        var result = await service.CreateProductAsync(
            new CreateProductRequest("Bread", type.Id, 1.25m, CreatePng(800, 400), "image/png", null, null),
            "user-id");

        Assert.Equal("image/png", result.PictureContentType);
        Assert.Equal(400, result.PictureWidth);
        Assert.Equal(200, result.PictureHeight);
        Assert.NotNull(result.Picture);
        using var imageStream = new MemoryStream(result.Picture);
        var decoded = await Image.LoadAsync(imageStream);
        Assert.Equal(400, decoded.Width);
        Assert.Equal(200, decoded.Height);
    }

    [Fact]
    public async Task Malformed_image_upload_is_reported_as_validation_error()
    {
        await using var db = CreateContext();
        var type = new ProductType { Name = "Food" };
        db.ProductTypes.Add(type);
        await db.SaveChangesAsync();
        var service = new ProductCatalogService(db, CreateConfiguration());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateProductAsync(
                new CreateProductRequest("Bread", type.Id, 1.25m, [1, 2, 3], "image/png", null, null),
                "user-id"));
    }

    [Fact]
    public async Task Image_above_pixel_limit_is_rejected_before_full_decode()
    {
        await using var db = CreateContext();
        var type = new ProductType { Name = "Food" };
        db.ProductTypes.Add(type);
        await db.SaveChangesAsync();
        var service = new ProductCatalogService(db, CreateConfiguration(("Image:MaxPixels", "100")));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateProductAsync(
                new CreateProductRequest("Bread", type.Id, 1.25m, CreatePng(20, 20), "image/png", null, null),
                "user-id"));
    }

    [Theory]
    [InlineData("A product name that is valid", 1.239)]
    [InlineData("An invalid name", -1)]
    public async Task Invalid_product_price_is_rejected(string name, decimal price)
    {
        await using var db = CreateContext();
        var type = new ProductType { Name = "Food" };
        db.ProductTypes.Add(type);
        await db.SaveChangesAsync();
        var service = new ProductCatalogService(db, CreateConfiguration());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateProductAsync(
                new CreateProductRequest(name, type.Id, price, null, null, null, null),
                "user-id"));
    }

    [Fact]
    public async Task Product_name_longer_than_database_column_is_rejected()
    {
        await using var db = CreateContext();
        var type = new ProductType { Name = "Food" };
        db.ProductTypes.Add(type);
        await db.SaveChangesAsync();
        var service = new ProductCatalogService(db, CreateConfiguration());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateProductAsync(
                new CreateProductRequest(new string('x', 201), type.Id, 1m, null, null, null, null),
                "user-id"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Image:MaxUploadSizeBytes"] = "5242880",
            ["Image:Width"] = "100",
            ["Image:Height"] = "100",
            ["Image:Quality"] = "85",
            ["Image:MaxPixels"] = "40000000"
        };
        foreach (var (key, value) in values)
        {
            settings[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }
}
