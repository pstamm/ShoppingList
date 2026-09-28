using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasConversion(v => v, v => v)
                .HasColumnType("datetimeoffset");

            entity.Property(e => e.UpdatedAt)
                .HasConversion(v => v, v => v)
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
