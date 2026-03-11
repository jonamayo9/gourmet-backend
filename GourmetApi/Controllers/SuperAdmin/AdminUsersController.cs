using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Dtos.SuperAdmin;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/admin-users")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminUsersController(AppDbContext db) => _db = db;

        // ✅ GET /api/superadmin/admin-users?companyId=1
        // - si viene companyId => solo CompanyAdmin de esa empresa
        // - si NO viene => trae todo (por si querés una pantalla global)
        [HttpGet]
        public async Task<ActionResult<List<AdminUserDto>>> GetAll([FromQuery] int? companyId)
        {
            var q = _db.AdminUsers.AsNoTracking().AsQueryable();

            if (companyId.HasValue && companyId.Value > 0)
            {
                q = q.Where(x =>
                    x.CompanyId == companyId.Value &&
                    x.Role == AdminRole.CompanyAdmin
                );
            }

            var list = await q
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminUserDto
                {
                    Id = x.Id,
                    Email = x.Email,
                    Enabled = x.Enabled,
                    Role = x.Role.ToString(),
                    CompanyId = x.CompanyId,
                    CreatedAt = x.CreatedAt,
                    LastLoginAt = x.LastLoginAt,

                    CanAccessOrders = x.CanAccessOrders,
                    CanAccessProducts = x.CanAccessProducts,
                    CanAccessCategories = x.CanAccessCategories,
                    CanAccessShifts = x.CanAccessShifts,
                    CanAccessDashboard = x.CanAccessDashboard,
                    CanAccessTablesWaiter = x.CanAccessTablesWaiter,
                    CanAccessTableConfig = x.CanAccessTableConfig,
                    CanAccessTableDashboard = x.CanAccessTableDashboard
                })
                .ToListAsync();

            return Ok(list);
        }

        // ✅ POST /api/superadmin/admin-users/company-admin
        [HttpPost("company-admin")]
        public async Task<ActionResult<AdminUserDto>> CreateCompanyAdmin([FromBody] CreateCompanyAdminDto dto)
        {
            var email = (dto.Email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email requerido.");
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4) return BadRequest("Password inválida.");
            if (dto.CompanyId <= 0) return BadRequest("CompanyId inválido.");

            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == dto.CompanyId);
            if (company == null) return BadRequest("CompanyId inexistente.");

            var exists = await _db.AdminUsers.AnyAsync(x => x.Email.ToLower() == email);
            if (exists) return Conflict("Ya existe un usuario con ese email.");

            var u = new AdminUser
            {
                Email = email,
                Enabled = dto.Enabled,
                Role = AdminRole.CompanyAdmin,
                CompanyId = dto.CompanyId,
                CreatedAt = DateTime.UtcNow,

                CanAccessOrders = company.FeatureOrdersEnabled && dto.CanAccessOrders,
                CanAccessProducts = company.FeatureProductsEnabled && dto.CanAccessProducts,
                CanAccessCategories = company.FeatureCategoriesEnabled && dto.CanAccessCategories,
                CanAccessShifts = company.FeatureShiftsEnabled && dto.CanAccessShifts,
                CanAccessDashboard = company.FeatureDashboardEnabled && dto.CanAccessDashboard,
                CanAccessTablesWaiter = company.FeatureTableManagementEnabled && dto.CanAccessTablesWaiter,
                CanAccessTableConfig = company.FeatureTableManagementEnabled && dto.CanAccessTableConfig,
                CanAccessTableDashboard = company.FeatureTableManagementEnabled && dto.CanAccessTableDashboard,
            };

            // ✅ usar SIEMPRE PasswordHasher (porque tu Auth usa PasswordHasher)
            var hasher = new PasswordHasher<AdminUser>();
            u.PasswordHash = hasher.HashPassword(u, dto.Password);

            _db.AdminUsers.Add(u);
            await _db.SaveChangesAsync();

            return Ok(ToDto(u));
        }

        // ✅ PUT /api/superadmin/admin-users/{id}/enabled
        [HttpPut("{id:int}/enabled")]
        public async Task<ActionResult> SetEnabled([FromRoute] int id, [FromBody] SetEnabledDto dto)
        {
            var u = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound("Usuario inexistente.");

            // seguridad: no tocar superadmins desde este ABM (opcional pero recomendable)
            if (u.Role == AdminRole.SuperAdmin) return BadRequest("No se puede modificar un SuperAdmin desde acá.");

            u.Enabled = dto.Enabled;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ✅ PUT /api/superadmin/admin-users/{id}/password
        [HttpPut("{id:int}/password")]
        public async Task<ActionResult> ResetPassword([FromRoute] int id, [FromBody] SetPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4)
                return BadRequest("Password inválida.");

            var u = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound("Usuario inexistente.");

            if (u.Role == AdminRole.SuperAdmin) return BadRequest("No se puede cambiar la password de un SuperAdmin desde acá.");

            var hasher = new PasswordHasher<AdminUser>();
            u.PasswordHash = hasher.HashPassword(u, dto.Password);

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ✅ PUT /api/superadmin/admin-users/{id}/permissions
        [HttpPut("{id:int}/permissions")]
        public async Task<ActionResult> UpdatePermissions([FromRoute] int id, [FromBody] UpdateAdminUserPermissionsDto dto)
        {
            var u = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound("Usuario inexistente.");

            if (u.Role == AdminRole.SuperAdmin)
                return BadRequest("No se pueden modificar permisos de un SuperAdmin desde acá.");

            if (!u.CompanyId.HasValue)
                return BadRequest("El usuario no tiene empresa asociada.");

            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == u.CompanyId.Value);
            if (company == null)
                return BadRequest("Empresa inexistente.");

            u.CanAccessOrders = company.FeatureOrdersEnabled && dto.CanAccessOrders;
            u.CanAccessProducts = company.FeatureProductsEnabled && dto.CanAccessProducts;
            u.CanAccessCategories = company.FeatureCategoriesEnabled && dto.CanAccessCategories;
            u.CanAccessShifts = company.FeatureShiftsEnabled && dto.CanAccessShifts;
            u.CanAccessDashboard = company.FeatureDashboardEnabled && dto.CanAccessDashboard;
            u.CanAccessTablesWaiter = company.FeatureTableManagementEnabled && dto.CanAccessTablesWaiter;
            u.CanAccessTableConfig = company.FeatureTableManagementEnabled && dto.CanAccessTableConfig;
            u.CanAccessTableDashboard = company.FeatureTableManagementEnabled && dto.CanAccessTableDashboard;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static AdminUserDto ToDto(AdminUser x) => new AdminUserDto
        {
            Id = x.Id,
            Email = x.Email,
            Enabled = x.Enabled,
            Role = x.Role.ToString(),
            CompanyId = x.CompanyId,
            CreatedAt = x.CreatedAt,
            LastLoginAt = x.LastLoginAt,

            CanAccessOrders = x.CanAccessOrders,
            CanAccessProducts = x.CanAccessProducts,
            CanAccessCategories = x.CanAccessCategories,
            CanAccessShifts = x.CanAccessShifts,
            CanAccessDashboard = x.CanAccessDashboard,
            CanAccessTablesWaiter = x.CanAccessTablesWaiter,
            CanAccessTableConfig = x.CanAccessTableConfig,
            CanAccessTableDashboard = x.CanAccessTableDashboard
        };
    }
}