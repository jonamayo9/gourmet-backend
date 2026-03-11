namespace GourmetApi.Dtos;

public class AddManualTableItemRequestDto
{
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}