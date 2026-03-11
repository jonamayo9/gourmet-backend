using GourmetApi.Entities;

public class TableSessionItem
{
    public int Id { get; set; }

    public int TableSessionId { get; set; }
    public TableSession TableSession { get; set; } = null!;

    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public string Name { get; set; } = "";
    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string? Note { get; set; }

    public bool IsManual { get; set; }
    public bool IsInternalProduct { get; set; }
    public bool IsDiscount { get; set; }
    public bool SentToKitchen { get; set; }
    public DateTime? SentToKitchenAt { get; set; }

    public Order? Order { get; set; }
    public int? OrderId { get; set; }
}