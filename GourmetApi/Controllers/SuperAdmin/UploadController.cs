using GourmetApi.Dtos.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GourmetApi.Controllers.SuperAdmin;

[ApiController]
[Route("api/superadmin/uploads")]
[Authorize(Roles = "SuperAdmin")]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public UploadsController(IWebHostEnvironment env) => _env = env;

    [HttpPost("image")]
    [Consumes("multipart/form-data")] // ✅ CLAVE para swagger
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequestDto req)
    {
        var file = req.File;

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo requerido" });

        var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Formato no permitido (png/jpg/webp)" });

        var folder = Path.Combine(_env.WebRootPath, "uploads", "companies");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

        var name = $"logo_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, name);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        var url = $"/uploads/companies/{name}";
        return Ok(new { url });
    }
}