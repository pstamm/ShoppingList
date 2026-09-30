using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.ApiTests;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task Saves_insert_update_soft_delete_and_hard_delete_audit_records()
    {
        await using var db = CreateContext();
        var productType = new ProductType
        {
            Name = "Pantry",
            CreatedByUserId = "user-123"
        };
        db.ProductTypes.Add(productType);
        await db.SaveChangesAsync();

        var insertAudit = Assert.Single(await db.Audit.Where(audit => audit.Operation == "Insert").ToListAsync());
        Assert.Equal("ProductTypes", insertAudit.TableName);
        Assert.Equal(productType.Id.ToString(), insertAudit.RecordId);
        Assert.Equal("user-123", insertAudit.UserId);
        Assert.Equal("{}", insertAudit.Before);
        using (var changes = JsonDocument.Parse(insertAudit.After))
        {
            Assert.Equal("Pantry", changes.RootElement.GetProperty("Name").GetString());
            Assert.Equal(productType.Id, changes.RootElement.GetProperty("Id").GetInt32());
        }

        productType.Name = "Groceries";
        await db.SaveChangesAsync();
        var updateAudit = Assert.Single(await db.Audit.Where(audit => audit.Operation == "Update").ToListAsync());
        using (var changes = JsonDocument.Parse(updateAudit.After))
        {
            Assert.Single(changes.RootElement.EnumerateObject());
            Assert.Equal("Groceries", changes.RootElement.GetProperty("Name").GetString());
        }
        using (var before = JsonDocument.Parse(updateAudit.Before))
        {
            Assert.Single(before.RootElement.EnumerateObject());
            Assert.Equal("Pantry", before.RootElement.GetProperty("Name").GetString());
        }

        productType.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var softDeleteAudit = Assert.Single(await db.Audit.Where(audit => audit.Operation == "SoftDelete").ToListAsync());
        using (var changes = JsonDocument.Parse(softDeleteAudit.After))
        {
            Assert.True(changes.RootElement.TryGetProperty("DeletedAt", out _));
        }
        using (var before = JsonDocument.Parse(softDeleteAudit.Before))
        {
            Assert.True(before.RootElement.TryGetProperty("DeletedAt", out var previousDeletedAt));
            Assert.Equal(JsonValueKind.Null, previousDeletedAt.ValueKind);
        }

        db.ProductTypes.Remove(productType);
        await db.SaveChangesAsync();
        var deleteAudit = Assert.Single(await db.Audit.Where(audit => audit.Operation == "Delete").ToListAsync());
        Assert.Equal("{}", deleteAudit.After);
        Assert.Equal(productType.Id.ToString(), deleteAudit.RecordId);
        using (var before = JsonDocument.Parse(deleteAudit.Before))
        {
            Assert.Equal("Groceries", before.RootElement.GetProperty("Name").GetString());
            var entityPropertyNames = db.Entry(productType).Properties
                .Select(property => property.Metadata.Name)
                .OrderBy(name => name, StringComparer.Ordinal);
            var beforePropertyNames = before.RootElement.EnumerateObject()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal);
            Assert.Equal(entityPropertyNames, beforePropertyNames);
        }
        Assert.Equal(4, await db.Audit.CountAsync());
    }

    [Fact]
    public async Task Does_not_create_audit_records_for_audit_table_changes()
    {
        await using var db = CreateContext();
        db.Audit.Add(new Audit
        {
            TableName = "Manual",
            RecordId = "1",
            Operation = "Insert",
            OperationDate = DateTimeOffset.UtcNow,
            After = "{}"
        });

        await db.SaveChangesAsync();

        Assert.Single(await db.Audit.ToListAsync());
    }

    private static ApplicationDbContext CreateContext()
    {
        var interceptor = new AuditSaveChangesInterceptor(new HttpContextAccessor());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }
}
