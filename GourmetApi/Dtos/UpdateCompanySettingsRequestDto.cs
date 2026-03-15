namespace GourmetApi.Dtos
{
    public class UpdateCompanySettingsRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Whatsapp { get; set; }
        public string? LogoUrl { get; set; }
        public string? Alias { get; set; }

        public decimal TransferSurchargePercent { get; set; }
        public decimal MercadoPagoSurchargePercent { get; set; }
    }
}
