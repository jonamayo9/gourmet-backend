namespace GourmetApi.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int MenuItemId { get; set; }

    public int Qty { get; set; }

    // Snapshot (lo guardamos como estaba en el momento)
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string? Note { get; set; }
}