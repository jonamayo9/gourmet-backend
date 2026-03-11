namespace GourmetApi.Dtos
{
    public class CreateMercadoPagoOrderResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public string InitPoint { get; set; } = "";
    }
}
