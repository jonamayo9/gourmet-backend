using GourmetApi.Entities;

namespace GourmetApi.Dtos.Orders
{
    public class AdminCreateOrderDto
    {
        public string CustomerName { get; set; } = "";
        public string? CustomerPhone { get; set; }
        public string? Address { get; set; }
        public string OrderType { get; set; } = "Delivery";
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? Notes { get; set; }
        public List<AdminCreateOrderItemDto> Items { get; set; } = new();
    }

    public class AdminCreateOrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }
}