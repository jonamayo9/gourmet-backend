namespace GourmetApi.Dtos.SuperAdmin
{
    public class SetEnabledDto
    {
        public bool Enabled { get; set; }
    }

    public class SetPasswordDto
    {
        public string Password { get; set; } = null!;
    }
}