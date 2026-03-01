namespace GourmetApi.Entities;

public class Order
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public string OrderNumber { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CustomerName { get; set; } = "";
    public string Address { get; set; } = "";

    public string PaymentMethod { get; set; } = "";

    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public OrderStatus Status { get; set; } = OrderStatus.New;
}