namespace GourmetApi.Dtos;

public class ShiftDto
{
    public int Id { get; set; }
    public int? DayOfWeek { get; set; }
    public int OpenHour { get; set; }
    public int CloseHour { get; set; }
    public bool Enabled { get; set; }
}