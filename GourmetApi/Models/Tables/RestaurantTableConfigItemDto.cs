namespace GourmetApi.Models.Tables
{
    public class RestaurantTableConfigItemDto
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }
    }
}