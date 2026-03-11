namespace GourmetApi.Dtos;

public class TableSessionAddMenuItemRequestDto
{
    public int MenuItemId { get; set; }
    public int Qty { get; set; }
    public string? Note { get; set; }
}