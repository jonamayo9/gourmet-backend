namespace GourmetApi.Dtos
{

    public class CreateMercadoPagoOrderItemRequest
    {
        public int MenuItemId { get; set; }
        public int Qty { get; set; }
        public string? Note { get; set; }
    }
}
