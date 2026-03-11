namespace GourmetApi.Dtos;

public class PublicMenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public bool VisibleInPublicMenu { get; set; } = true;
    public bool VisibleInTables { get; set; } = true;
    public bool IsInternalForTables { get; set; } = false;
}