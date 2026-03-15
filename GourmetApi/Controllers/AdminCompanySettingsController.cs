using GourmetApi.Data;
using GourmetApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/admin/{companySlug}/company-settings")]
    public class AdminCompanySettingsController : ControllerBase
    {
        private const long MaxLogoSizeBytes = 3 * 1024 * 1024; // 3 MB

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public AdminCompanySettingsController(
            AppDbContext db,
            IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<CompanySettingsResponseDto>> Get(string companySlug)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var hasAccess = await HasAccess(companySlug);
            if (!hasAccess)
                return Forbid();

            var dto = new CompanySettingsResponseDto
            {
                Name = company.Name,
                Whatsapp = company.Whatsapp,
                LogoUrl = company.LogoUrl,
                Alias = company.Alias,
                TransferSurchargePercent = company.TransferSurchargePercent,
                MercadoPagoSurchargePercent = company.MercadoPagoSurchargePercent,
                TransferEnabled = company.TransferEnabled,
                MercadoPagoEnabled = company.MercadoPagoEnabled
            };

            return Ok(dto);
        }

        [HttpPut]
        public async Task<ActionResult<CompanySettingsResponseDto>> Put(
            string companySlug,
            [FromBody] UpdateCompanySettingsRequestDto request)
        {
            if (request == null)
                return BadRequest("Body requerido.");

            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var hasAccess = await HasAccess(companySlug);
            if (!hasAccess)
                return Forbid();

            var name = request.Name?.Trim();
            var whatsapp = string.IsNullOrWhiteSpace(request.Whatsapp) ? null : request.Whatsapp.Trim();
            var logoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
            var alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("El nombre es obligatorio.");

            if (name.Length > 150)
                return BadRequest("El nombre no puede superar los 150 caracteres.");

            if (whatsapp != null && whatsapp.Length > 50)
                return BadRequest("El WhatsApp no puede superar los 50 caracteres.");

            if (logoUrl != null && logoUrl.Length > 500)
                return BadRequest("La URL del logo no puede superar los 500 caracteres.");

            if (alias != null && alias.Length > 100)
                return BadRequest("El alias no puede superar los 100 caracteres.");

            if (request.TransferSurchargePercent < 0 || request.TransferSurchargePercent > 100)
                return BadRequest("El recargo por transferencia debe estar entre 0 y 100.");

            if (request.MercadoPagoSurchargePercent < 0 || request.MercadoPagoSurchargePercent > 100)
                return BadRequest("El recargo de Mercado Pago debe estar entre 0 y 100.");

            company.Name = name;
            company.Whatsapp = whatsapp;
            company.LogoUrl = logoUrl;

            if (company.TransferEnabled)
            {
                company.Alias = alias;
                company.TransferSurchargePercent = request.TransferSurchargePercent;
            }

            if (company.MercadoPagoEnabled)
            {
                company.MercadoPagoSurchargePercent = request.MercadoPagoSurchargePercent;
            }

            await _db.SaveChangesAsync();

            var dto = new CompanySettingsResponseDto
            {
                Name = company.Name,
                Whatsapp = company.Whatsapp,
                LogoUrl = company.LogoUrl,
                Alias = company.Alias,
                TransferSurchargePercent = company.TransferSurchargePercent,
                MercadoPagoSurchargePercent = company.MercadoPagoSurchargePercent,
                TransferEnabled = company.TransferEnabled,
                MercadoPagoEnabled = company.MercadoPagoEnabled
            };

            return Ok(dto);
        }

        [HttpPost("logo")]
        public async Task<ActionResult<UploadCompanyLogoResponseDto>> UploadLogo(
            string companySlug,
            IFormFile file)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var hasAccess = await HasAccess(companySlug);
            if (!hasAccess)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("Debe enviar un archivo.");

            if (file.Length > MaxLogoSizeBytes)
                return BadRequest("El archivo no puede superar los 3 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Formato no permitido. Solo se acepta jpg, jpeg, png o webp.");

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(
                webRootPath,
                "uploads",
                "company-logos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{companySlug}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var logoUrl = $"/uploads/company-logos/{fileName}";

            company.LogoUrl = logoUrl;
            await _db.SaveChangesAsync();

            return Ok(new UploadCompanyLogoResponseDto
            {
                LogoUrl = logoUrl
            });
        }

        private async Task<bool> HasAccess(string companySlug)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
                return false;

            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return false;

            var adminUser = await _db.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == email &&
                    x.CompanyId == company.Id &&
                    x.Enabled);

            if (adminUser == null)
                return false;

            return adminUser.CanAccessCompanySettings;
        }
    }
}