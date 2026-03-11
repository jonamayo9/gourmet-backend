namespace GourmetApi.Models.Tables
{
    public class AddTableItemProductDto
    {
        public int MenuItemId { get; set; }
        public int Qty { get; set; }
        public string? Note { get; set; }
    }
}