using GourmetApi.Entities;
using GourmetApi.Enums;

public class TableSession
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public int RestaurantTableId { get; set; }
    public RestaurantTable RestaurantTable { get; set; }

    public TableSessionStatus Status { get; set; }

    public int TotalGuests { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }

    public string? Notes { get; set; }

    public decimal Total { get; set; }

    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public List<TableSessionItem> Items { get; set; } = new();
}