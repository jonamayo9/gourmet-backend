namespace GourmetApi.Dtos
{
    public class CreateCompanyAdminDto
    {
        public int CompanyId { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool Enabled { get; set; } = true;
    }
}