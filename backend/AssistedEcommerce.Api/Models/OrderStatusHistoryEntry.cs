namespace AssistedEcommerce.Api.Models;

public class OrderStatusHistoryEntry
{
    public string Status { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string By { get; set; } = "system";
    public string? Note { get; set; }
}
