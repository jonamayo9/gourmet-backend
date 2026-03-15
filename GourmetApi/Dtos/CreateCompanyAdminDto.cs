namespace GourmetApi.Dtos
{
    public class CreateCompanyAdminDto
    {
        public int CompanyId { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool Enabled { get; set; } = true;

        public bool CanAccessOrders { get; set; } = false;
        public bool CanAccessProducts { get; set; } = false;
        public bool CanAccessCategories { get; set; } = false;
        public bool CanAccessShifts { get; set; } = false;
        public bool CanAccessDashboard { get; set; } = false;
        public bool FeatureMenuOnlyEnabled { get; set; }
        public bool FeatureTableManagementEnabled { get; set; }

        public bool TablesEnabled { get; set; }
        public bool EnableGuestCount { get; set; }
        public bool EnableAdultsChildrenSplit { get; set; }
        public bool RequireAdultsChildrenSplit { get; set; }
        public bool CanAccessTablesWaiter { get; set; }
        public bool CanAccessTableConfig { get; set; }
        public bool CanAccessTableDashboard { get; set; }
        public bool CanAccessCompanySettings { get; set; }
    }
}