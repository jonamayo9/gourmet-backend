namespace GourmetApi.Dtos
{
        public class CompanySettingsResponseDto
        {
            public string Name { get; set; } = null!;
            public string? Whatsapp { get; set; }
            public string? LogoUrl { get; set; }
            public string? Alias { get; set; }

            public decimal TransferSurchargePercent { get; set; }
            public decimal MercadoPagoSurchargePercent { get; set; }

            public bool TransferEnabled { get; set; }
            public bool MercadoPagoEnabled { get; set; }
        }

    public class UploadCompanyLogoResponseDto
    {
        public string LogoUrl { get; set; } = null!;
    }
}
