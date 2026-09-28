namespace ShoppingApi.Domain.Entities;

public class ListShare
{
    public int Id { get; set; }

    public int ListId { get; set; }
    public ShoppingList? List { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
