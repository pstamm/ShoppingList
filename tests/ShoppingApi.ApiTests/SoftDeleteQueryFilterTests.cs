using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.ApiTests;

public class SoftDeleteQueryFilterTests
{
    [Fact]
    public async Task Soft_deleted_records_are_excluded_from_all_soft_deletable_entity_queries()
    {
        await using var db = CreateContext();
        await SeedRecordsAsync(db, deletedAt: null, suffix: "active");
        await SeedRecordsAsync(db, deletedAt: DateTimeOffset.UtcNow, suffix: "deleted");

        Assert.Single(await db.ProductTypes.ToListAsync());
        Assert.Single(await db.Products.ToListAsync());
        Assert.Single(await db.ShoppingLists.ToListAsync());
        Assert.Single(await db.ListProducts.ToListAsync());
        Assert.Single(await db.ListShares.ToListAsync());

        Assert.Equal(2, await db.ProductTypes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.Products.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.ShoppingLists.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.ListProducts.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.ListShares.IgnoreQueryFilters().CountAsync());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedRecordsAsync(
        ApplicationDbContext db,
        DateTimeOffset? deletedAt,
        string suffix)
    {
        var user = new ApplicationUser
        {
            Id = $"user-{suffix}",
            UserName = $"user-{suffix}",
            NormalizedUserName = $"USER-{suffix}".ToUpperInvariant()
        };
        var productType = new ProductType
        {
            Name = $"Type {suffix}",
            DeletedAt = deletedAt
        };
        var product = new Product
        {
            Name = $"Product {suffix}",
            ProductType = productType,
            Price = 1m,
            DeletedAt = deletedAt
        };
        var list = new ShoppingList
        {
            Name = $"List {suffix}",
            OwnerUserId = user.Id,
            Owner = user,
            DeletedAt = deletedAt
        };
        var listProduct = new ListProduct
        {
            List = list,
            Product = product,
            DeletedAt = deletedAt
        };
        var share = new ListShare
        {
            List = list,
            User = user,
            DeletedAt = deletedAt
        };

        db.Users.Add(user);
        db.ProductTypes.Add(productType);
        db.Products.Add(product);
        db.ShoppingLists.Add(list);
        db.ListProducts.Add(listProduct);
        db.ListShares.Add(share);
        await db.SaveChangesAsync();
    }
}
