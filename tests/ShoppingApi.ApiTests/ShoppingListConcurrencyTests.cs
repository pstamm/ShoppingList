using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.ShoppingLists;
using ShoppingApi.Services.ShoppingLists;

namespace ShoppingApi.ApiTests;

public class ShoppingListConcurrencyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("AQID")]
    public async Task List_update_rejects_missing_or_malformed_row_versions(string rowVersion)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var list = new ShoppingApi.Domain.Entities.ShoppingList
        {
            Name = "Weekly",
            OwnerUserId = "owner-id"
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync();
        var service = new ShoppingListService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateListAsync(
                list.Id,
                new UpdateShoppingListRequest("Renamed", rowVersion),
                "owner-id",
                isAdmin: false));
    }
}
