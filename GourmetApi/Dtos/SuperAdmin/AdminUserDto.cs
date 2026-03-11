namespace GourmetApi.Dtos.SuperAdmin
{
    public class AdminUserDto
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public bool Enabled { get; set; }

        public string Role { get; set; } = null!;

        public int? CompanyId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool CanAccessOrders { get; set; } = false;
        public bool CanAccessProducts { get; set; } = false;
        public bool CanAccessCategories { get; set; } = false;
        public bool CanAccessShifts { get; set; } = false;
        public bool CanAccessDashboard { get; set; } = false;
        public bool CanAccessTablesWaiter { get; set; }
        public bool CanAccessTableConfig { get; set; }
        public bool CanAccessTableDashboard { get; set; }
    }
}