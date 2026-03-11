using GourmetApi.Entities;

public class RestaurantTable
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public int Number { get; set; }
    public string? Name { get; set; }
    public int Capacity { get; set; }

    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}