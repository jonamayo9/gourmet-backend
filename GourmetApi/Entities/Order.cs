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

    public decimal SubtotalBase { get; set; }
    public decimal PaymentSurchargePercent { get; set; }
    public decimal PaymentSurchargeAmount { get; set; }

    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public string? PaymentProvider { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.None;
    public string? LastPaymentId { get; set; }

    public int? TableSessionId { get; set; }
    public int? RestaurantTableId { get; set; }
    public string? TableName { get; set; }
    public bool IsTableOrder { get; set; }
    public bool WaiterNotified { get; set; }

    public string Source { get; set; } = "Public"; // Public | Admin | Table

    public string? QrPayload { get; set; } // opcional
    public string? QrReference { get; set; } // opcional
}