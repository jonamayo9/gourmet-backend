using GourmetApi.Data;
using GourmetApi.Dtos.Admin;
using MercadoPago.Resource.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GourmetApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/me")]
    [Authorize]
    public class AdminMeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminMeController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<AdminMeDto>> GetMe()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email")
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var admin = await _db.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());

            if (admin == null)
                return NotFound("Admin no encontrado.");

            if (admin.CompanyId == null)
            {
                return Ok(new AdminMeDto
                {
                    Email = admin.Email,
                    CompanyId = null,
                    CompanySlug = null,
                    CompanyName = null,

                    FeatureOrdersEnabled = true,
                    FeatureProductsEnabled = true,
                    FeatureCategoriesEnabled = true,
                    FeatureShiftsEnabled = true,
                    FeatureDashboardEnabled = true,
                    MercadoPagoEnabled = true,

                    CanAccessOrders = admin.CanAccessOrders,
                    CanAccessProducts = admin.CanAccessProducts,
                    CanAccessCategories = admin.CanAccessCategories,
                    CanAccessShifts = admin.CanAccessShifts,
                    CanAccessDashboard = admin.CanAccessDashboard,
                    CanAccessTablesWaiter = admin.CanAccessTablesWaiter,
                    CanAccessTableConfig = admin.CanAccessTableConfig,
                    CanAccessTableDashboard = admin.CanAccessTableDashboard
                });
            }

            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == admin.CompanyId.Value);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            return Ok(new AdminMeDto
            {
                Email = admin.Email,
                CompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name,

                FeatureOrdersEnabled = company.FeatureOrdersEnabled,
                FeatureProductsEnabled = company.FeatureProductsEnabled,
                FeatureCategoriesEnabled = company.FeatureCategoriesEnabled,
                FeatureShiftsEnabled = company.FeatureShiftsEnabled,
                FeatureDashboardEnabled = company.FeatureDashboardEnabled,
                MercadoPagoEnabled = company.MercadoPagoEnabled,

                CanAccessOrders = admin.CanAccessOrders,
                CanAccessProducts = admin.CanAccessProducts,
                CanAccessCategories = admin.CanAccessCategories,
                CanAccessShifts = admin.CanAccessShifts,
                CanAccessDashboard = admin.CanAccessDashboard,
                CanAccessTablesWaiter = admin.CanAccessTablesWaiter,
                CanAccessTableConfig = admin.CanAccessTableConfig,
                CanAccessTableDashboard = admin.CanAccessTableDashboard,
                FeatureTableManagementEnabled = company.FeatureTableManagementEnabled,
            });
        }
    }
}