namespace GourmetApi.Dtos.Admin
{
    public class AdminMeDto
    {
        public string Email { get; set; } = "";
        public string LogoUrl { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanySlug { get; set; }
        public string? CompanyName { get; set; }

        public bool FeatureOrdersEnabled { get; set; }
        public bool FeatureProductsEnabled { get; set; }
        public bool FeatureCategoriesEnabled { get; set; }
        public bool FeatureShiftsEnabled { get; set; }
        public bool FeatureDashboardEnabled { get; set; }

        public bool MercadoPagoEnabled { get; set; }

        public bool CanAccessOrders { get; set; }
        public bool CanAccessProducts { get; set; }
        public bool CanAccessCategories { get; set; }
        public bool CanAccessShifts { get; set; }
        public bool CanAccessDashboard { get; set; }
        public bool CanAccessTablesWaiter { get; set; }
        public bool CanAccessTableConfig { get; set; }
        public bool CanAccessTableDashboard { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }
    }
}