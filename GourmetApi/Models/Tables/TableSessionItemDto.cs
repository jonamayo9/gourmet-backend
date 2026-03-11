namespace GourmetApi.Models.Tables
{
    public class TableSessionItemDto
    {
        public int Id { get; set; }
        public int? MenuItemId { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string? Note { get; set; }

        public bool IsManual { get; set; }
        public bool IsInternalProduct { get; set; }
        public bool IsDiscount { get; set; }
        public bool SentToKitchen { get; set; }
        public DateTime? SentToKitchenAt { get; set; }

        public string? KitchenStatus { get; set; }
        public bool IsFinished { get; set; }
    }
}