namespace GourmetApi.Models.Tables
{
    public class PayTableSessionDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
    }
}