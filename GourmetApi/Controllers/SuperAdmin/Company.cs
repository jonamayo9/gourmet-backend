using GourmetApi.Data;
using GourmetApi.Dtos.SuperAdmin;
using GourmetApi.Entities;
using GourmetApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/companies")]
    [Authorize(Roles = "SuperAdmin")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly CloudinaryService? _cloudinary;

        public CompaniesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var list = await _db.Companies.AsNoTracking()
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Select(x => new CompanyDto
                    {
                        Id = x.Id,
                        Slug = x.Slug,
                        Name = x.Name,
                        Whatsapp = x.Whatsapp,
                        Alias = x.Alias,
                        LogoUrl = x.LogoUrl,
                        Enabled = x.Enabled,
                        CreatedAtUtc = x.CreatedAtUtc
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompanyDto>> GetById(int id)
        {
            var x = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (x == null) return NotFound();

            return Ok(new CompanyDto
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                Whatsapp = x.Whatsapp,
                Alias = x.Alias,
                LogoUrl = x.LogoUrl,
                Enabled = x.Enabled,
                CreatedAtUtc = x.CreatedAtUtc
            });
        }

        [HttpPost]
        public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyDto dto)
        {
            var slug = (dto.Slug ?? "").Trim().ToLowerInvariant();
            var name = (dto.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(slug)) return BadRequest("Slug requerido.");
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name requerido.");

            var exists = await _db.Companies.AnyAsync(x => x.Slug == slug);
            if (exists) return Conflict("Ya existe una empresa con ese slug.");

            var c = new Company
            {
                Slug = slug,
                Name = name,
                Whatsapp = dto.Whatsapp,
                Alias = dto.Alias,
                LogoUrl = dto.LogoUrl,
                Enabled = dto.Enabled,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Companies.Add(c);
            await _db.SaveChangesAsync();

            return Ok(new CompanyDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Name = c.Name,
                Whatsapp = c.Whatsapp,
                Alias = c.Alias,
                LogoUrl = c.LogoUrl,
                Enabled = c.Enabled,
                CreatedAtUtc = c.CreatedAtUtc
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto)
        {
            var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            var name = (dto.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name requerido.");

            c.Name = name;
            c.Whatsapp = dto.Whatsapp;
            c.Alias = dto.Alias;
            c.LogoUrl = dto.LogoUrl;
            c.Enabled = dto.Enabled;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/logo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UploadLogo(int id, [FromForm] UploadLogoForm form)
        {
            if (_cloudinary == null)
                return StatusCode(500, "Cloudinary no configurado");

            var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound("Company not found");

            var file = form.File;
            if (file == null || file.Length == 0) return BadRequest("Archivo requerido");
            if (!file.ContentType.StartsWith("image/")) return BadRequest("Debe ser imagen");

            var folder = $"menuonline/companies/{c.Slug}/logo";

            var url = await _cloudinary.UploadImageAsync(file, folder);

            c.LogoUrl = url;
            await _db.SaveChangesAsync();

            return Ok(new { url });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            _db.Companies.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UploadLogoForm
    {
        public IFormFile File { get; set; } = default!;
    }
}