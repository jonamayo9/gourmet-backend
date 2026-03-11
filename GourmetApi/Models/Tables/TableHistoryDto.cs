using GourmetApi.Enums;

namespace GourmetApi.Models.Tables
{
    public class TableHistoryDto
    {
        public int SessionId { get; set; }

        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public string TableName { get; set; } = string.Empty;

        public TableSessionStatus Status { get; set; }

        public int TotalGuests { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }

        public decimal Total { get; set; }

        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public List<TableHistoryItemDto> Items { get; set; } = new();
    }
}