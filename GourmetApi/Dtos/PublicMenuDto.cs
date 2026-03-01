namespace GourmetApi.Dtos;

public class PublicMenuDto
{
    public string CompanySlug { get; set; }
    public string CompanyName { get; set; }
    public string LogoUrl { get; set; }
    public string Whatsapp { get; set; }
    public string Alias { get; set; }

    public List<PublicCategoryDto> Categories { get; set; } = new();
}