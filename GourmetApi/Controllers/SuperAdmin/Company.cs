using GourmetApi.Data;
using GourmetApi.Dtos.SuperAdmin;
using GourmetApi.Entities;
using GourmetApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace GourmetApi.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/companies")]
    [Authorize(Roles = "SuperAdmin")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IServiceProvider _serviceProvider;

        public CompaniesController(AppDbContext db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompanyDto>>> GetAll()
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
                        CreatedAtUtc = x.CreatedAtUtc,
                        MercadoPagoEnabled = x.MercadoPagoEnabled,
                        MercadoPagoHasToken = !string.IsNullOrWhiteSpace(x.MercadoPagoAccessToken),
                        MercadoPagoMaskedToken = string.IsNullOrWhiteSpace(x.MercadoPagoAccessToken)
                            ? null
                            : MaskToken(x.MercadoPagoAccessToken!),
                        FeatureOrdersEnabled = x.FeatureOrdersEnabled,
                        FeatureProductsEnabled = x.FeatureProductsEnabled,
                        FeatureCategoriesEnabled = x.FeatureCategoriesEnabled,
                        FeatureShiftsEnabled = x.FeatureShiftsEnabled,
                        FeatureDashboardEnabled = x.FeatureDashboardEnabled,
                        FeatureMenuOnlyEnabled = x.FeatureMenuOnlyEnabled,
                        FeatureTableManagementEnabled = x.FeatureTableManagementEnabled,
                        TablesEnabled = x.TablesEnabled,
                        EnableGuestCount = x.EnableGuestCount,
                        EnableAdultsChildrenSplit = x.EnableAdultsChildrenSplit,
                        RequireAdultsChildrenSplit = x.RequireAdultsChildrenSplit
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
                CreatedAtUtc = x.CreatedAtUtc,
                MercadoPagoEnabled = x.MercadoPagoEnabled,
                MercadoPagoHasToken = !string.IsNullOrWhiteSpace(x.MercadoPagoAccessToken),
                MercadoPagoMaskedToken = string.IsNullOrWhiteSpace(x.MercadoPagoAccessToken)
                    ? null
                    : MaskToken(x.MercadoPagoAccessToken!),
                FeatureOrdersEnabled = x.FeatureOrdersEnabled,
                FeatureProductsEnabled = x.FeatureProductsEnabled,
                FeatureCategoriesEnabled = x.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = x.FeatureShiftsEnabled,
                FeatureDashboardEnabled = x.FeatureDashboardEnabled,
                FeatureMenuOnlyEnabled = x.FeatureMenuOnlyEnabled,
                FeatureTableManagementEnabled = x.FeatureTableManagementEnabled,
                TablesEnabled = x.TablesEnabled,
                EnableGuestCount = x.EnableGuestCount,
                EnableAdultsChildrenSplit = x.EnableAdultsChildrenSplit,
                RequireAdultsChildrenSplit = x.RequireAdultsChildrenSplit

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
                CreatedAtUtc = DateTime.UtcNow,

                // NUEVO
                MercadoPagoEnabled = dto.MercadoPagoEnabled,
                MercadoPagoAccessToken = NormalizeToken(dto.MercadoPagoAccessToken),
                FeatureOrdersEnabled = dto.FeatureOrdersEnabled,
                FeatureProductsEnabled = dto.FeatureProductsEnabled,
                FeatureCategoriesEnabled = dto.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = dto.FeatureShiftsEnabled,
                FeatureDashboardEnabled = dto.FeatureDashboardEnabled,
                FeatureMenuOnlyEnabled = dto.FeatureMenuOnlyEnabled,
                FeatureTableManagementEnabled = dto.FeatureTableManagementEnabled,
                TablesEnabled = dto.TablesEnabled,
                EnableGuestCount = dto.EnableGuestCount,
                EnableAdultsChildrenSplit = dto.EnableAdultsChildrenSplit,
                RequireAdultsChildrenSplit = dto.RequireAdultsChildrenSplit
            };

            if (dto.FeatureMenuOnlyEnabled)
            {
                c.FeatureOrdersEnabled = false;
            }
            else
            {
                c.FeatureOrdersEnabled = dto.FeatureOrdersEnabled;
            }

            if (!dto.FeatureTableManagementEnabled)
            {
                dto.TablesEnabled = false;
                dto.EnableGuestCount = false;
                dto.EnableAdultsChildrenSplit = false;
                dto.RequireAdultsChildrenSplit = false;
            }

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
                CreatedAtUtc = c.CreatedAtUtc,
                MercadoPagoEnabled = c.MercadoPagoEnabled,
                MercadoPagoHasToken = !string.IsNullOrWhiteSpace(c.MercadoPagoAccessToken),
                MercadoPagoMaskedToken = string.IsNullOrWhiteSpace(c.MercadoPagoAccessToken)
                    ? null
                    : MaskToken(c.MercadoPagoAccessToken!),
                FeatureOrdersEnabled = dto.FeatureOrdersEnabled,
                FeatureProductsEnabled = dto.FeatureProductsEnabled,
                FeatureCategoriesEnabled = dto.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = dto.FeatureShiftsEnabled,
                FeatureDashboardEnabled = dto.FeatureDashboardEnabled,
                FeatureMenuOnlyEnabled = dto.FeatureMenuOnlyEnabled,
                FeatureTableManagementEnabled = dto.FeatureTableManagementEnabled,
                TablesEnabled = dto.TablesEnabled,
                EnableGuestCount = dto.EnableGuestCount,
                EnableAdultsChildrenSplit = dto.EnableAdultsChildrenSplit,
                RequireAdultsChildrenSplit = dto.RequireAdultsChildrenSplit
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

            // NUEVO
            c.MercadoPagoEnabled = dto.MercadoPagoEnabled;
            c.MercadoPagoAccessToken = NormalizeToken(dto.MercadoPagoAccessToken);
            c.FeatureOrdersEnabled = dto.FeatureOrdersEnabled;
            c.FeatureProductsEnabled = dto.FeatureProductsEnabled;
            c.FeatureCategoriesEnabled = dto.FeatureCategoriesEnabled;
            c.FeatureShiftsEnabled = dto.FeatureShiftsEnabled;
            c.FeatureDashboardEnabled = dto.FeatureDashboardEnabled;
            c.FeatureMenuOnlyEnabled = dto.FeatureMenuOnlyEnabled;
            c.FeatureTableManagementEnabled = dto.FeatureTableManagementEnabled;
            c.TablesEnabled = dto.TablesEnabled;
            c.EnableGuestCount = dto.EnableGuestCount;
            c.EnableAdultsChildrenSplit = dto.EnableAdultsChildrenSplit;
            c.RequireAdultsChildrenSplit = dto.RequireAdultsChildrenSplit;

            if (dto.FeatureMenuOnlyEnabled)
            {
                c.FeatureOrdersEnabled = false;
            }
            else
            {
                c.FeatureOrdersEnabled = dto.FeatureOrdersEnabled;
            }

            if (!dto.FeatureTableManagementEnabled)
            {
                dto.TablesEnabled = false;
                dto.EnableGuestCount = false;
                dto.EnableAdultsChildrenSplit = false;
                dto.RequireAdultsChildrenSplit = false;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/logo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UploadLogo(int id, [FromForm] UploadLogoForm form)
        {
            try
            {
                var cloudinary = _serviceProvider.GetService<CloudinaryService>();
                if (cloudinary == null)
                    return StatusCode(500, "Cloudinary no configurado");

                var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
                if (c == null) return NotFound("Company not found");

                var file = form.File;
                if (file == null || file.Length == 0) return BadRequest("Archivo requerido");
                if (!file.ContentType.StartsWith("image/")) return BadRequest("Debe ser imagen");

                var folder = $"menuonline/companies/{c.Slug}/logo";
                var url = await cloudinary.UploadImageAsync(file, folder);

                c.LogoUrl = url;
                await _db.SaveChangesAsync();

                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
                if (company == null)
                    return NotFound();

                var adminUsers = await _db.AdminUsers
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                if (adminUsers.Count > 0)
                    _db.AdminUsers.RemoveRange(adminUsers);

                var orders = await _db.Orders
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                var items = await _db.Set<MenuItem>()
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                var categories = await _db.Categories
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                var shifts = await _db.Set<Shift>()
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                if (orders.Count > 0)
                    _db.Orders.RemoveRange(orders);

                if (items.Count > 0)
                    _db.Set<MenuItem>().RemoveRange(items);

                if (categories.Count > 0)
                    _db.Categories.RemoveRange(categories);

                if (shifts.Count > 0)
                    _db.Set<Shift>().RemoveRange(shifts);

                _db.Companies.Remove(company);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, ex.ToString());
            }
        }

        private static string? NormalizeToken(string? token)
        {
            var value = token?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string MaskToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "";
            if (token.Length <= 8) return "********";
            return token.Substring(0, 8) + "********";
        }
    }

    public class UploadLogoForm
    {
        public IFormFile File { get; set; } = default!;
    }
}