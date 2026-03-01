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
    }

    public class CreateCompanyDto
    {
        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class UpdateCompanyDto
    {
        public string Name { get; set; } = null!;
        public string? Whatsapp { get; set; }
        public string? Alias { get; set; }
        public string? LogoUrl { get; set; }
        public bool Enabled { get; set; } = true;
    }
}