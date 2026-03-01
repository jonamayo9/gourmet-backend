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
    }
}