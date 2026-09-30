using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data.Configurations;

public class ListProductConfiguration : IEntityTypeConfiguration<ListProduct>
{
    public void Configure(EntityTypeBuilder<ListProduct> builder)
    {
        builder.ToTable("ListProducts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipicalOrder).HasColumnType("int");
        builder.Property(x => x.ToOrderNow).HasColumnType("int");
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAt == null); // use IgnoreQueryFilters() when a query intentionally needs deleted records

        builder.HasIndex(x => new { x.ListId, x.ProductId })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasOne(x => x.List)
            .WithMany(x => x.ListProducts)
            .HasForeignKey(x => x.ListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ListProducts)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
