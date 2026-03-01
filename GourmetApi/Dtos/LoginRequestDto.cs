namespace GourmetApi.Dtos.Auth
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        // opcional: si es CompanyAdmin lo mandás, si es SuperAdmin puede venir null/empty
        public string? CompanySlug { get; set; }
    }
}