using System.ComponentModel.DataAnnotations;

namespace GourmetApi.Entities
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = null!;

        // Guardamos hash, no password plano
        [Required]
        public string PasswordHash { get; set; } = null!;

        public bool Enabled { get; set; } = true;

        // 👇 nuevo: rol
        public AdminRole Role { get; set; } = AdminRole.CompanyAdmin;

        // 👇 nuevo: null = SuperAdmin
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}