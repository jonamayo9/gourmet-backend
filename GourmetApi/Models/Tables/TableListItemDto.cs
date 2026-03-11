namespace GourmetApi.Models.Tables
{
    public class TableListItemDto
    {
        public int TableId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }

        public string Status { get; set; } = "Free";

        public int? SessionId { get; set; }
        public int? TotalGuests { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }

        public decimal CurrentTotal { get; set; }

        public DateTime? OpenedAt { get; set; }
    }
}