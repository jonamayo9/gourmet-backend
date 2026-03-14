namespace GourmetApi.Entities;

public class MercadoPagoCheckout
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CustomerName { get; set; } = "";
    public string Address { get; set; } = "";

    public decimal SubtotalBase { get; set; }
    public decimal PaymentSurchargePercent { get; set; }
    public decimal PaymentSurchargeAmount { get; set; }
    public decimal Total { get; set; }

    public string PayloadJson { get; set; } = "";

    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected | Cancelled

    public string? PreferenceId { get; set; }
    public string? PaymentId { get; set; }

    public int? OrderId { get; set; }
}