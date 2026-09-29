using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShoppingApi.Data;

namespace ShoppingApi.ApiTests;

public class DatabaseMigrationTests
{
    [Fact]
    public void Initial_migration_generates_sql_server_schema_script()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=ShoppingMigrationTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new ApplicationDbContext(options);

        var migrations = db.Database.GetMigrations().ToList();
        var script = db.GetService<IMigrator>().GenerateScript();

        Assert.Contains("20260929091723_InitialSchema", migrations);
        Assert.Contains("CREATE TABLE [AspNetUsers]", script);
        Assert.Contains("CREATE TABLE [Products]", script);
        Assert.Contains("CREATE TABLE [ShoppingLists]", script);
        Assert.Contains("rowversion", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE [DeletedAt] IS NULL", script, StringComparison.OrdinalIgnoreCase);
    }
}
