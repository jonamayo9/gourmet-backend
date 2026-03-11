namespace GourmetApi.Entities
{
    public class Company
    {
        public int Id { get; set; }

        // IMPORTANTE: slug debe ser único
        public string Slug { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }

        // nuevo
        public bool Enabled { get; set; } = true;

        // nuevo
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // NUEVO - Mercado Pago por empresa
        public bool MercadoPagoEnabled { get; set; } = false;
        public string? MercadoPagoAccessToken { get; set; }

        // Flags
        public bool FeatureOrdersEnabled { get; set; } = true;
        public bool FeatureProductsEnabled { get; set; } = true;
        public bool FeatureCategoriesEnabled { get; set; } = true;
        public bool FeatureShiftsEnabled { get; set; } = true;
        public bool FeatureDashboardEnabled { get; set; } = true;
        public bool FeatureMenuOnlyEnabled { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // (opcional, agregalos solo si existen estas entidades en tu proyecto)
        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

        public bool TablesEnabled { get; set; } = false;
        public bool EnableGuestCount { get; set; } = true;
        public bool EnableAdultsChildrenSplit { get; set; } = false;
        public bool RequireAdultsChildrenSplit { get; set; } = false;

    }
}