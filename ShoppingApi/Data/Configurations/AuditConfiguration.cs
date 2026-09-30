using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data.Configurations;

public sealed class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("Audit", table =>
            table.HasCheckConstraint(
                "CK_Audit_Operation",
                "[Operation] IN ('Insert', 'Update', 'Delete', 'SoftDelete')"));

        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.UserId).HasMaxLength(450);
        builder.Property(audit => audit.TableName).IsRequired().HasMaxLength(128);
        builder.Property(audit => audit.RecordId).IsRequired();
        builder.Property(audit => audit.Operation).IsRequired().HasMaxLength(20);
        builder.Property(audit => audit.OperationDate).HasColumnType("datetimeoffset");
        builder.Property(audit => audit.Before)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasColumnOrder(6);
        builder.Property(audit => audit.After)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}")
            .HasColumnOrder(7);
    }
}
