using GourmetApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GourmetApi.Controllers;

[ApiController]
[Route("api/admin/{companySlug}/uploads")]
public class UploadsController : ControllerBase
{
    private readonly CloudinaryService _cloud;
    public UploadsController(CloudinaryService cloud) => _cloud = cloud;

    [Authorize]
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(string companySlug, [FromForm] UploadImageForm form)
    {
        var file = form.File;
        if (file == null || file.Length == 0) return BadRequest("Archivo requerido");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Debe ser imagen");

        var url = await _cloud.UploadImageAsync(file, $"menuonline/items/{companySlug}");
        return Ok(new { url });
    }
}

public class UploadImageForm
{
    public IFormFile File { get; set; } = default!;
}