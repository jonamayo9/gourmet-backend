namespace GourmetApi.Dtos;

public class ShiftUpsertDto
{
    public int? DayOfWeek { get; set; }
    public int OpenHour { get; set; }
    public int CloseHour { get; set; }
    public bool Enabled { get; set; } = true;
}