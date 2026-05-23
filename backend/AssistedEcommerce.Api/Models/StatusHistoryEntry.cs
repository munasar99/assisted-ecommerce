namespace AssistedEcommerce.Api.Models;

public class StatusHistoryEntry
{
    public string Status { get; set; } = string.Empty;
    public DateTime At { get; set; }
    public string? By { get; set; }
    public string? Note { get; set; }
}
