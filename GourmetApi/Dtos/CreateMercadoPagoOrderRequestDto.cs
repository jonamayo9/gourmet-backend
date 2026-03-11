namespace GourmetApi.Dtos
{
    public class CreateMercadoPagoOrderRequest
    {
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public List<CreateMercadoPagoOrderItemRequest> Items { get; set; } = new();
    }
}
