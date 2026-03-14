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
                var list = await _db.Companies
                    .AsNoTracking()
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

                        TransferSurchargeEnabled = x.TransferSurchargeEnabled,
                        TransferSurchargePercent = x.TransferSurchargePercent,
                        MercadoPagoSurchargeEnabled = x.MercadoPagoSurchargeEnabled,
                        MercadoPagoSurchargePercent = x.MercadoPagoSurchargePercent,

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
                        RequireAdultsChildrenSplit = x.RequireAdultsChildrenSplit,
                        TransferEnabled = x.TransferEnabled
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
            if (x == null)
            {
                return NotFound();
            }

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

                TransferSurchargeEnabled = x.TransferSurchargeEnabled,
                TransferSurchargePercent = x.TransferSurchargePercent,
                MercadoPagoSurchargeEnabled = x.MercadoPagoSurchargeEnabled,
                MercadoPagoSurchargePercent = x.MercadoPagoSurchargePercent,

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
                RequireAdultsChildrenSplit = x.RequireAdultsChildrenSplit,
                TransferEnabled = x.TransferEnabled
            });
        }

        [HttpPost]
        public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyDto dto)
        {
            var slug = (dto.Slug ?? "").Trim().ToLowerInvariant();
            var name = (dto.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest("Slug requerido.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name requerido.");
            }

            if (dto.TransferSurchargePercent < -100m || dto.TransferSurchargePercent > 100m)
            {
                return BadRequest("El porcentaje de transferencia debe estar entre -100 y 100.");
            }

            if (dto.MercadoPagoSurchargePercent < -100m || dto.MercadoPagoSurchargePercent > 100m)
            {
                return BadRequest("El porcentaje de Mercado Pago debe estar entre -100 y 100.");
            }

            var exists = await _db.Companies.AnyAsync(x => x.Slug == slug);
            if (exists)
            {
                return Conflict("Ya existe una empresa con ese slug.");
            }

            var c = new Company
            {
                Slug = slug,
                Name = name,
                Whatsapp = string.IsNullOrWhiteSpace(dto.Whatsapp) ? null : dto.Whatsapp.Trim(),
                Alias = string.IsNullOrWhiteSpace(dto.Alias) ? null : dto.Alias.Trim(),
                LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim(),
                Enabled = dto.Enabled,
                CreatedAtUtc = DateTime.UtcNow,

                MercadoPagoEnabled = dto.MercadoPagoEnabled,
                MercadoPagoAccessToken = NormalizeToken(dto.MercadoPagoAccessToken),

                TransferSurchargeEnabled = dto.TransferSurchargeEnabled,
                TransferSurchargePercent = dto.TransferSurchargeEnabled ? dto.TransferSurchargePercent : 0m,
                MercadoPagoSurchargeEnabled = dto.MercadoPagoSurchargeEnabled,
                MercadoPagoSurchargePercent = dto.MercadoPagoSurchargeEnabled ? dto.MercadoPagoSurchargePercent : 0m,

                FeatureOrdersEnabled = dto.FeatureOrdersEnabled,
                FeatureProductsEnabled = dto.FeatureProductsEnabled,
                FeatureCategoriesEnabled = dto.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = dto.FeatureShiftsEnabled,
                FeatureDashboardEnabled = dto.FeatureDashboardEnabled,
                FeatureMenuOnlyEnabled = dto.FeatureMenuOnlyEnabled,
                FeatureTableManagementEnabled = dto.FeatureTableManagementEnabled,
                TransferEnabled = dto.TransferEnabled,
                TablesEnabled = dto.FeatureTableManagementEnabled ? dto.TablesEnabled : false,
                EnableGuestCount = dto.FeatureTableManagementEnabled ? dto.EnableGuestCount : false,
                EnableAdultsChildrenSplit = dto.FeatureTableManagementEnabled ? dto.EnableAdultsChildrenSplit : false,
                RequireAdultsChildrenSplit = dto.FeatureTableManagementEnabled
                    ? (dto.EnableAdultsChildrenSplit ? dto.RequireAdultsChildrenSplit : false)
                    : false
            };

            if (c.FeatureMenuOnlyEnabled)
            {
                c.FeatureOrdersEnabled = false;
            }

            if (!c.FeatureTableManagementEnabled)
            {
                c.TablesEnabled = false;
                c.EnableGuestCount = false;
                c.EnableAdultsChildrenSplit = false;
                c.RequireAdultsChildrenSplit = false;
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

                TransferSurchargeEnabled = c.TransferSurchargeEnabled,
                TransferSurchargePercent = c.TransferSurchargePercent,
                MercadoPagoSurchargeEnabled = c.MercadoPagoSurchargeEnabled,
                MercadoPagoSurchargePercent = c.MercadoPagoSurchargePercent,

                FeatureOrdersEnabled = c.FeatureOrdersEnabled,
                FeatureProductsEnabled = c.FeatureProductsEnabled,
                FeatureCategoriesEnabled = c.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = c.FeatureShiftsEnabled,
                FeatureDashboardEnabled = c.FeatureDashboardEnabled,
                FeatureMenuOnlyEnabled = c.FeatureMenuOnlyEnabled,
                FeatureTableManagementEnabled = c.FeatureTableManagementEnabled,

                TablesEnabled = c.TablesEnabled,
                EnableGuestCount = c.EnableGuestCount,
                EnableAdultsChildrenSplit = c.EnableAdultsChildrenSplit,
                RequireAdultsChildrenSplit = c.RequireAdultsChildrenSplit
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto)
        {
            var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
            {
                return NotFound();
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest("Name requerido.");
                }

                c.Name = name;
            }

            if (dto.Whatsapp is not null)
            {
                c.Whatsapp = string.IsNullOrWhiteSpace(dto.Whatsapp)
                    ? null
                    : dto.Whatsapp.Trim();
            }

            if (dto.Alias is not null)
            {
                c.Alias = string.IsNullOrWhiteSpace(dto.Alias)
                    ? null
                    : dto.Alias.Trim();
            }

            if (dto.ClearLogo)
            {
                c.LogoUrl = null;
            }
            else if (dto.LogoUrl is not null)
            {
                c.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl)
                    ? null
                    : dto.LogoUrl.Trim();
            }

            if (dto.Enabled.HasValue)
            {
                c.Enabled = dto.Enabled.Value;
            }

            if (dto.MercadoPagoEnabled.HasValue)
            {
                c.MercadoPagoEnabled = dto.MercadoPagoEnabled.Value;
            }

            if (dto.ClearMercadoPagoAccessToken)
            {
                c.MercadoPagoAccessToken = null;
            }
            else if (dto.MercadoPagoAccessToken is not null)
            {
                var newToken = NormalizeToken(dto.MercadoPagoAccessToken);

                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    c.MercadoPagoAccessToken = newToken;
                }
            }

            if (dto.TransferSurchargeEnabled.HasValue)
            {
                c.TransferSurchargeEnabled = dto.TransferSurchargeEnabled.Value;
            }

            if (dto.TransferSurchargePercent.HasValue)
            {
                if (dto.TransferSurchargePercent.Value < -100m || dto.TransferSurchargePercent.Value > 100m)
                {
                    return BadRequest("El porcentaje de transferencia debe estar entre -100 y 100.");
                }

                c.TransferSurchargePercent = dto.TransferSurchargePercent.Value;
            }

            if (dto.MercadoPagoSurchargeEnabled.HasValue)
            {
                c.MercadoPagoSurchargeEnabled = dto.MercadoPagoSurchargeEnabled.Value;
            }

            if (dto.MercadoPagoSurchargePercent.HasValue)
            {
                if (dto.MercadoPagoSurchargePercent.Value < -100m || dto.MercadoPagoSurchargePercent.Value > 100m)
                {
                    return BadRequest("El porcentaje de Mercado Pago debe estar entre -100 y 100.");
                }

                c.MercadoPagoSurchargePercent = dto.MercadoPagoSurchargePercent.Value;
            }

            if (dto.TransferEnabled.HasValue)
            {
                c.TransferEnabled = dto.TransferEnabled.Value;
            }

            if (dto.FeatureProductsEnabled.HasValue)
            {
                c.FeatureProductsEnabled = dto.FeatureProductsEnabled.Value;
            }

            if (dto.FeatureCategoriesEnabled.HasValue)
            {
                c.FeatureCategoriesEnabled = dto.FeatureCategoriesEnabled.Value;
            }

            if (dto.FeatureShiftsEnabled.HasValue)
            {
                c.FeatureShiftsEnabled = dto.FeatureShiftsEnabled.Value;
            }

            if (dto.FeatureDashboardEnabled.HasValue)
            {
                c.FeatureDashboardEnabled = dto.FeatureDashboardEnabled.Value;
            }

            if (dto.FeatureMenuOnlyEnabled.HasValue)
            {
                c.FeatureMenuOnlyEnabled = dto.FeatureMenuOnlyEnabled.Value;
            }

            if (dto.FeatureTableManagementEnabled.HasValue)
            {
                c.FeatureTableManagementEnabled = dto.FeatureTableManagementEnabled.Value;
            }

            if (dto.FeatureOrdersEnabled.HasValue)
            {
                c.FeatureOrdersEnabled = dto.FeatureOrdersEnabled.Value;
            }

            if (c.FeatureMenuOnlyEnabled)
            {
                c.FeatureOrdersEnabled = false;
            }

            if (!c.FeatureTableManagementEnabled)
            {
                c.TablesEnabled = false;
                c.EnableGuestCount = false;
                c.EnableAdultsChildrenSplit = false;
                c.RequireAdultsChildrenSplit = false;
            }
            else
            {
                if (dto.TablesEnabled.HasValue)
                {
                    c.TablesEnabled = dto.TablesEnabled.Value;
                }

                if (dto.EnableGuestCount.HasValue)
                {
                    c.EnableGuestCount = dto.EnableGuestCount.Value;
                }

                if (dto.EnableAdultsChildrenSplit.HasValue)
                {
                    c.EnableAdultsChildrenSplit = dto.EnableAdultsChildrenSplit.Value;
                }

                if (dto.RequireAdultsChildrenSplit.HasValue)
                {
                    c.RequireAdultsChildrenSplit = dto.RequireAdultsChildrenSplit.Value;
                }

                if (!c.EnableAdultsChildrenSplit)
                {
                    c.RequireAdultsChildrenSplit = false;
                }
            }

            if (!c.TransferSurchargeEnabled)
            {
                c.TransferSurchargePercent = 0m;
            }

            if (!c.MercadoPagoSurchargeEnabled)
            {
                c.MercadoPagoSurchargePercent = 0m;
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
                {
                    return StatusCode(500, "Cloudinary no configurado");
                }

                var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
                if (c == null)
                {
                    return NotFound("Company not found");
                }

                var file = form.File;
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Archivo requerido");
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    return BadRequest("Debe ser imagen");
                }

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
                {
                    return NotFound();
                }

                var adminUsers = await _db.AdminUsers
                    .Where(x => x.CompanyId == id)
                    .ToListAsync();

                if (adminUsers.Count > 0)
                {
                    _db.AdminUsers.RemoveRange(adminUsers);
                }

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
                {
                    _db.Orders.RemoveRange(orders);
                }

                if (items.Count > 0)
                {
                    _db.Set<MenuItem>().RemoveRange(items);
                }

                if (categories.Count > 0)
                {
                    _db.Categories.RemoveRange(categories);
                }

                if (shifts.Count > 0)
                {
                    _db.Set<Shift>().RemoveRange(shifts);
                }

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
            if (string.IsNullOrWhiteSpace(token))
            {
                return "";
            }

            if (token.Length <= 8)
            {
                return "********";
            }

            return token.Substring(0, 8) + "********";
        }
    }

    public class UploadLogoForm
    {
        public IFormFile File { get; set; } = default!;
    }
}