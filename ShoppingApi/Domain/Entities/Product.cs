namespace ShoppingApi.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ProductTypeId { get; set; }
    public ProductType? ProductType { get; set; }

    public decimal Price { get; set; }

    public byte[]? Picture { get; set; }
    public string? PictureContentType { get; set; }
    public int? PictureWidth { get; set; }
    public int? PictureHeight { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ListProduct> ListProducts { get; set; } = new List<ListProduct>();
}
