using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data.Configurations;

public class ListShareConfiguration : IEntityTypeConfiguration<ListShare>
{
    public void Configure(EntityTypeBuilder<ListShare> builder)
    {
        builder.ToTable("ListShares");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => x.DeletedAt == null); // use IgnoreQueryFilters() when a query intentionally needs deleted records

        builder.HasIndex(x => new { x.ListId, x.UserId })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasOne(x => x.List)
            .WithMany(x => x.Shares)
            .HasForeignKey(x => x.ListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
