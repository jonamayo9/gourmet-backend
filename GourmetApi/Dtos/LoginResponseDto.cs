namespace GourmetApi.Dtos.Auth
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public int ExpiresInMinutes { get; set; }

        // opcional (te sirve para el front)
        public string Role { get; set; } = null!;
        public int? CompanyId { get; set; }
        public string? CompanySlug { get; set; }
    }
}