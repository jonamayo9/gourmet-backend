using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GourmetApi.Controllers;

[ApiController]
[Route("api/admin/{companySlug}/uploads")]
public class UploadsController : ControllerBase
{
    [Authorize]
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(string companySlug, [FromForm] UploadImageForm form)
    {
        var file = form.File;

        if (file == null || file.Length == 0) return BadRequest("Archivo requerido");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Debe ser imagen");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "items");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"/uploads/items/{fileName}";
        return Ok(new { url });
    }
}

public class UploadImageForm
{
    public IFormFile File { get; set; } = default!;
}