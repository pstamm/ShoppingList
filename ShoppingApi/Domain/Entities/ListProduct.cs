namespace ShoppingApi.Domain.Entities;

public class ListProduct
{
    public int Id { get; set; }

    public int ListId { get; set; }
    public ShoppingList? List { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int TipicalOrder { get; set; }
    public int ToOrderNow { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
