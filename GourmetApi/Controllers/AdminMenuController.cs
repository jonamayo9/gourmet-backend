using GourmetApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GourmetApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/admin/{companySlug}/menu-for-tables")]
    [Authorize]
    public class AdminMenuController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminMenuController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> GetMenuForTables([FromRoute] string companySlug)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var categories = await _db.Categories
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == company.Id &&
                    x.Enabled &&
                    x.VisibleInTables)
               .OrderBy(x => x.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    items = _db.MenuItems
                        .Where(i =>
                            i.CategoryId == c.Id &&
                            i.Enabled &&
                            !i.IsDeleted &&
                            i.VisibleInTables)
                        .OrderBy(i => i.Name)
                        .Select(i => new
                        {
                            id = i.Id,
                            name = i.Name,
                            price = i.Price,
                            image = i.ImageUrl,
                            internalProduct = i.IsInternalForTables
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }
    }
}
