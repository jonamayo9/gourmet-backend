namespace GourmetApi.Entities
{
    public class Company
    {
        public int Id { get; set; }

        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;

        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }

        public bool Enabled { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool MercadoPagoEnabled { get; set; } = false;
        public string? MercadoPagoAccessToken { get; set; }

        // RECARGOS POR MÉTODO DE PAGO
        public bool TransferSurchargeEnabled { get; set; } = false;
        public decimal TransferSurchargePercent { get; set; } = 0m;

        public bool MercadoPagoSurchargeEnabled { get; set; } = false;
        public decimal MercadoPagoSurchargePercent { get; set; } = 0m;

        public bool FeatureOrdersEnabled { get; set; } = true;
        public bool FeatureProductsEnabled { get; set; } = true;
        public bool FeatureCategoriesEnabled { get; set; } = true;
        public bool FeatureShiftsEnabled { get; set; } = true;
        public bool FeatureDashboardEnabled { get; set; } = true;
        public bool FeatureMenuOnlyEnabled { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

        public bool TablesEnabled { get; set; } = false;
        public bool EnableGuestCount { get; set; } = true;
        public bool EnableAdultsChildrenSplit { get; set; } = false;
        public bool RequireAdultsChildrenSplit { get; set; } = false;
        public decimal CashSurchargePercent { get; set; }
        public decimal QrSurchargePercent { get; set; }
        public bool TransferEnabled { get; set; } = false;
    }
}