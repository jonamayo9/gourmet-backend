using System.ComponentModel.DataAnnotations;

namespace GourmetApi.Dtos.SuperAdmin
{
    public class UploadImageRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}