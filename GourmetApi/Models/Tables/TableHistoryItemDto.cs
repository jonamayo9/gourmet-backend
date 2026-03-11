namespace GourmetApi.Models.Tables
{
    public class TableHistoryItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Note { get; set; }
        public bool IsManual { get; set; }
        public bool IsInternalProduct { get; set; }
    }
}