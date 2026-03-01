namespace GourmetApi.Entities;

public class Shift
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; }

    public int? DayOfWeek { get; set; }

    public int OpenHour { get; set; }
    public int CloseHour { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}