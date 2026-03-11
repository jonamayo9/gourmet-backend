namespace GourmetApi.Dtos;

public class PublicMenuDto
{
    public string CompanySlug { get; set; }
    public string CompanyName { get; set; }
    public string LogoUrl { get; set; }
    public string Whatsapp { get; set; }
    public string Alias { get; set; }
    public bool MenuOnlyEnabled { get; set; }
    public bool TableManagementEnabled { get; set; }
    public List<PublicCategoryDto> Categories { get; set; } = new();
    public bool VisibleInPublicMenu { get; set; } = true;
    public bool VisibleInTables { get; set; } = true;
    public bool IsInternalForTables { get; set; } = false;
}