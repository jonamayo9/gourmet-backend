namespace GourmetApi.Entities
{
    public class Category
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

        public string Name { get; set; }
        public int SortOrder { get; set; }

        public bool Enabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool VisibleInPublicMenu { get; set; } = true;
        public bool VisibleInTables { get; set; } = true;

        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
    }
}
