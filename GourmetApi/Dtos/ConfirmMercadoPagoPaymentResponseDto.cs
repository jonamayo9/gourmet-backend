namespace GourmetApi.Dtos
{
    public class ConfirmMercadoPagoPaymentResponse
    {
        public bool Ok { get; set; }
        public bool Approved { get; set; }
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? Message { get; set; }
        public string? WhatsappUrl { get; set; }
        public string? StorePhone { get; set; }
    }
}