namespace GourmetApi.Dtos.SuperAdmin
{
    public class UpdateAdminUserPermissionsDto
    {
        public bool CanAccessOrders { get; set; }
        public bool CanAccessProducts { get; set; }
        public bool CanAccessCategories { get; set; }
        public bool CanAccessShifts { get; set; }
        public bool CanAccessDashboard { get; set; }
        public bool CanAccessTablesWaiter { get; set; }
        public bool CanAccessTableConfig { get; set; }
        public bool CanAccessTableDashboard { get; set; }
        public bool CanAccessCompanySettings { get; set; }
    }
}