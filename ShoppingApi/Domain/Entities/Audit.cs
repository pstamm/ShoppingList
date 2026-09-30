namespace ShoppingApi.Domain.Entities;

public class Audit
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTimeOffset OperationDate { get; set; }
    public string After { get; set; } = "{}";
    public string Before { get; set; } = "{}";
}
