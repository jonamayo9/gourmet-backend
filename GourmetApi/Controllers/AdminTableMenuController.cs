using GourmetApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/{companySlug}/table-menu")]
public class AdminTableMenuController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminTableMenuController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicTableMenu([FromRoute] string companySlug)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == companySlug && x.Enabled);

        if (company == null)
            return NotFound(new { message = "Empresa no encontrada" });

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.CompanyId == company.Id && c.Enabled)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                sortOrder = c.SortOrder
            })
            .ToListAsync();

        var items = await _db.MenuItems.AsNoTracking()
            .Where(i =>
                i.CompanyId == company.Id &&
                i.Enabled &&
                !i.IsDeleted &&
                i.VisibleInTables &&
                i.VisibleInPublicMenu &&
                !i.IsInternalForTables)
            .Join(_db.Categories.AsNoTracking(),
                i => i.CategoryId,
                c => c.Id,
                (i, c) => new { i, c })
            .Where(x => x.c.CompanyId == company.Id && x.c.Enabled)
            .OrderBy(x => x.c.SortOrder)
            .ThenBy(x => x.i.Name)
            .Select(x => new
            {
                id = x.i.Id,
                categoryId = x.i.CategoryId,
                categoryName = x.c.Name,
                name = x.i.Name,
                description = x.i.Description,
                price = x.i.Price,
                imageUrl = string.IsNullOrWhiteSpace(x.i.ImageUrl)
                    ? company.LogoUrl
                    : x.i.ImageUrl
            })
            .ToListAsync();

        return Ok(new
        {
            categories,
            items
        });
    }

    [HttpGet("hidden")]
    public async Task<IActionResult> GetHiddenTableMenu([FromRoute] string companySlug)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == companySlug && x.Enabled);

        if (company == null)
            return NotFound(new { message = "Empresa no encontrada" });

        var categories = await _db.Categories.AsNoTracking()
            .Where(x => x.CompanyId == company.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                sortOrder = c.SortOrder
            })
            .ToListAsync();

        var items = await _db.MenuItems.AsNoTracking()
            .Where(i =>
                i.CompanyId == company.Id &&
                i.Enabled &&
                !i.IsDeleted &&
                i.VisibleInTables &&
                (
                    !i.VisibleInPublicMenu ||
                    i.IsInternalForTables
                ))
            .Join(_db.Categories.AsNoTracking(),
                i => i.CategoryId,
                c => c.Id,
                (i, c) => new { i, c })
            .Where(x => x.c.CompanyId == company.Id)
            .OrderBy(x => x.c.SortOrder)
            .ThenBy(x => x.i.Name)
            .Select(x => new
            {
                id = x.i.Id,
                categoryId = x.i.CategoryId,
                categoryName = x.c.Name,
                name = x.i.Name,
                description = x.i.Description,
                price = x.i.Price,
                imageUrl = string.IsNullOrWhiteSpace(x.i.ImageUrl)
                    ? company.LogoUrl
                    : x.i.ImageUrl,
                visibleInPublicMenu = x.i.VisibleInPublicMenu,
                visibleInTables = x.i.VisibleInTables,
                isInternalForTables = x.i.IsInternalForTables
            })
            .ToListAsync();

        return Ok(new
        {
            categories,
            items
        });
    }
}