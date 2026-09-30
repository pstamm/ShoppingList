using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Picture).HasColumnType("varbinary(max)");
        builder.Property(x => x.PictureContentType).HasMaxLength(200);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAt == null); // use IgnoreQueryFilters() when a query intentionally needs deleted records

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasOne(x => x.ProductType)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ListProducts)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
