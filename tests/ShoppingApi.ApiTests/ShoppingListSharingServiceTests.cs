using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.ShoppingLists;
using ShoppingApi.Services.ShoppingLists;

namespace ShoppingApi.ApiTests;

public class ShoppingListSharingServiceTests
{
    [Fact]
    public async Task Owner_can_share_list_and_shared_user_can_view_and_edit_until_share_is_removed()
    {
        await using var db = CreateContext();
        var (list, product) = await SeedListAndProductAsync(db);
        var service = new ShoppingListService(db);

        var share = await service.CreateShareAsync(
            list.Id,
            new ShareListRequest("USER-B"),
            "user-a",
            isAdmin: false);

        Assert.Equal("user-b", share.UserId);
        Assert.Equal("user-b", share.UserName);
        Assert.Equal("user-b@example.com", share.Email);
        Assert.Single(await service.GetSharesAsync(list.Id, "user-a", isAdmin: false));
        Assert.Contains((await service.GetListsAsync("user-b", isAdmin: false)), item => item.Id == list.Id);
        Assert.NotNull(await service.GetListAsync(list.Id, "user-b", isAdmin: false));

        var listProduct = await service.AddListProductAsync(
            list.Id,
            new AddListProductRequest(product.Id, -1, 0, "shared note"),
            "user-b",
            isAdmin: false);
        Assert.Equal(-1, listProduct.TipicalOrder);
        Assert.Equal(0, listProduct.ToOrderNow);
        Assert.Equal("shared note", listProduct.Notes);
        Assert.Equal("Food", listProduct.ProductTypeName);
        Assert.Equal(2m, listProduct.ProductPrice);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateShareAsync(list.Id, new ShareListRequest("user-c"), "user-b", isAdmin: false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteListAsync(list.Id, string.Empty, "user-b", isAdmin: false));

        await service.RemoveShareAsync(list.Id, "user-b", "user-a", isAdmin: false);

        Assert.Empty(await service.GetSharesAsync(list.Id, "user-a", isAdmin: false));
        Assert.DoesNotContain((await service.GetListsAsync("user-b", isAdmin: false)), item => item.Id == list.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetListAsync(list.Id, "user-b", isAdmin: false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetListProductsAsync(list.Id, "user-b", isAdmin: false));

        await service.CreateShareAsync(list.Id, new ShareListRequest("user-b"), "user-a", isAdmin: false);
        Assert.NotNull(await service.GetListAsync(list.Id, "user-b", isAdmin: false));
    }

    [Fact]
    public async Task Share_creation_rejects_duplicates_owner_and_inactive_or_missing_users()
    {
        await using var db = CreateContext();
        var (list, _) = await SeedListAndProductAsync(db);
        var service = new ShoppingListService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateShareAsync(list.Id, new ShareListRequest("user-a"), "user-a", isAdmin: false));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateShareAsync(list.Id, new ShareListRequest("inactive-user"), "user-a", isAdmin: false));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateShareAsync(list.Id, new ShareListRequest("missing-user"), "user-a", isAdmin: false));

        await service.CreateShareAsync(list.Id, new ShareListRequest("user-b"), "user-a", isAdmin: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateShareAsync(list.Id, new ShareListRequest("user-b"), "user-a", isAdmin: false));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RemoveShareAsync(list.Id, "user-a", "user-a", isAdmin: false));
    }

    [Fact]
    public async Task Admin_can_access_and_manage_private_lists()
    {
        await using var db = CreateContext();
        var (list, _) = await SeedListAndProductAsync(db);
        var service = new ShoppingListService(db);

        Assert.NotNull(await service.GetListAsync(list.Id, "admin-id", isAdmin: true));
        var share = await service.CreateShareAsync(
            list.Id,
            new ShareListRequest("user-c"),
            "admin-id",
            isAdmin: true);
        Assert.Equal("user-c", share.UserId);

        await service.RemoveShareAsync(list.Id, "user-c", "admin-id", isAdmin: true);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(ShoppingList list, Product product)> SeedListAndProductAsync(
        ApplicationDbContext db)
    {
        db.Users.AddRange(
            new ApplicationUser { Id = "user-a", UserName = "user-a", NormalizedUserName = "USER-A", Email = "user-a@example.com" },
            new ApplicationUser { Id = "user-b", UserName = "user-b", NormalizedUserName = "USER-B", Email = "user-b@example.com" },
            new ApplicationUser { Id = "user-c", UserName = "user-c", NormalizedUserName = "USER-C", Email = "user-c@example.com" },
            new ApplicationUser
            {
                Id = "inactive-user",
                UserName = "inactive-user",
                NormalizedUserName = "INACTIVE-USER",
                Email = "inactive@example.com",
                IsActive = false
            });
        var productType = new ProductType { Name = "Food" };
        db.ProductTypes.Add(productType);
        var list = new ShoppingList
        {
            Name = "Weekly",
            OwnerUserId = "user-a",
            CreatedByUserId = "user-a",
            UpdatedByUserId = "user-a"
        };
        var product = new Product
        {
            Name = "Bread",
            ProductType = productType,
            Price = 2m,
            CreatedByUserId = "user-a",
            UpdatedByUserId = "user-a"
        };
        db.ShoppingLists.Add(list);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (list, product);
    }
}
