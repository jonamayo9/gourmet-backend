namespace GourmetApi.Dtos.SuperAdmin
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        // NUEVO
        public bool MercadoPagoEnabled { get; set; }
        public bool MercadoPagoHasToken { get; set; }
        public string? MercadoPagoMaskedToken { get; set; }

        // Flags
        public bool FeatureOrdersEnabled { get; set; }
        public bool FeatureProductsEnabled { get; set; }
        public bool FeatureCategoriesEnabled { get; set; }
        public bool FeatureShiftsEnabled { get; set; }
        public bool FeatureDashboardEnabled { get; set; }
        public bool FeatureMenuOnlyEnabled { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }

        public bool TablesEnabled { get; set; }
        public bool EnableGuestCount { get; set; }
        public bool EnableAdultsChildrenSplit { get; set; }
        public bool RequireAdultsChildrenSplit { get; set; }
    }

    public class CreateCompanyDto
    {
        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }
        public bool Enabled { get; set; } = true;

        // NUEVO
        public bool MercadoPagoEnabled { get; set; } = false;
        public string? MercadoPagoAccessToken { get; set; }

        // Flags
        public bool FeatureOrdersEnabled { get; set; }
        public bool FeatureProductsEnabled { get; set; }
        public bool FeatureCategoriesEnabled { get; set; }
        public bool FeatureShiftsEnabled { get; set; }
        public bool FeatureDashboardEnabled { get; set; }
        public bool FeatureMenuOnlyEnabled { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }
        public bool TablesEnabled { get; set; }
        public bool EnableGuestCount { get; set; }
        public bool EnableAdultsChildrenSplit { get; set; }
        public bool RequireAdultsChildrenSplit { get; set; }
    }

    public class UpdateCompanyDto
    {
        public string? Name { get; set; }
        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }
        public bool ClearLogo { get; set; }

        public bool? Enabled { get; set; }

        public bool? MercadoPagoEnabled { get; set; }
        public string? MercadoPagoAccessToken { get; set; }
        public bool ClearMercadoPagoAccessToken { get; set; }

        public bool? FeatureOrdersEnabled { get; set; }
        public bool? FeatureProductsEnabled { get; set; }
        public bool? FeatureCategoriesEnabled { get; set; }
        public bool? FeatureShiftsEnabled { get; set; }
        public bool? FeatureDashboardEnabled { get; set; }
        public bool? FeatureMenuOnlyEnabled { get; set; }
        public bool? FeatureTableManagementEnabled { get; set; }

        public bool? TablesEnabled { get; set; }
        public bool? EnableGuestCount { get; set; }
        public bool? EnableAdultsChildrenSplit { get; set; }
        public bool? RequireAdultsChildrenSplit { get; set; }
    }
}