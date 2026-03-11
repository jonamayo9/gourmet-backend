namespace GourmetApi.Models.Tables
{
    public class OpenTableRequestDto
    {
        public int TotalGuests { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public string? Notes { get; set; }
    }
}