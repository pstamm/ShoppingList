using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data.Configurations;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ListProduct> ListProducts { get; set; }
    public DbSet<ListShare> ListShares { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ProductTypeConfiguration());
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfiguration(new ShoppingListConfiguration());
        builder.ApplyConfiguration(new ListProductConfiguration());
        builder.ApplyConfiguration(new ListShareConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetimeoffset");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetimeoffset");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");

            entity.HasIndex(e => e.UserName)
                .IsUnique()
                .HasFilter("[UserName] IS NOT NULL");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });
    }
}
