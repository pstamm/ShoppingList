namespace ShoppingApi.Domain.Entities;

public class ShoppingList
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ListProduct> ListProducts { get; set; } = new List<ListProduct>();
    public ICollection<ListShare> Shares { get; set; } = new List<ListShare>();
}
