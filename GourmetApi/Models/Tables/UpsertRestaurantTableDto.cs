namespace GourmetApi.Models.Tables
{
    public class UpsertRestaurantTableDto
    {
        public int Number { get; set; }
        public string? Name { get; set; }
        public int Capacity { get; set; }
        public bool Enabled { get; set; } = true;
        public int Order { get; set; }
    }
}