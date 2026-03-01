namespace GourmetApi.Models
{
    public class OrderCreateRequestDto
    {
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public List<OrderItemCreateRequestDto> Items { get; set; } = new();
    }
}
