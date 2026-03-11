namespace GourmetApi.Models
{
    public class OrderItemCreateRequestDto
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public string? Note { get; set; }
    }
}
