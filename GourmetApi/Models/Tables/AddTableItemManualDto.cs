namespace GourmetApi.Models.Tables
{
    public class AddTableItemManualDto
    {
        public string Name { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Note { get; set; }
    }
}