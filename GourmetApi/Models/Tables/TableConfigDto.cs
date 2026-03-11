namespace GourmetApi.Models.Tables
{
    public class TableConfigDto
    {
        public bool TablesEnabled { get; set; }
        public bool EnableGuestCount { get; set; }
        public bool EnableAdultsChildrenSplit { get; set; }
        public bool RequireAdultsChildrenSplit { get; set; }
    }
}