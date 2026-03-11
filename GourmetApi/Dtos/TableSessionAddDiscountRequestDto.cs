namespace GourmetApi.Dtos;

public class TableSessionAddDiscountRequestDto
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}