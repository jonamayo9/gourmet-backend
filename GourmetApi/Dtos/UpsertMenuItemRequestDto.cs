namespace GourmetApi.Dtos
{
    public class UpsertMenuItemRequestDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public bool Enabled { get; set; } = true;
        public string? ImageUrl { get; set; }
        public bool VisibleInPublicMenu { get; set; } = true;
        public bool VisibleInTables { get; set; } = true;
        public bool IsInternalForTables { get; set; } = false;
    }
}