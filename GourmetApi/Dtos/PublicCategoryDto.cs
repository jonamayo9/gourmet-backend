namespace GourmetApi.Dtos;

public class PublicCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public List<PublicMenuItemDto> Items { get; set; } = new();
}