using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/{companySlug}")]
public class AdminMenuItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMenuItemsController(AppDbContext db)
    {
        _db = db;
    }

    // =========================
    // GET
    // =========================
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(string companySlug)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var items = await _db.MenuItems
            .AsNoTracking()
            .Where(i => i.CompanyId == company.Id && !i.IsDeleted)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.CategoryId,
                i.Name,
                i.Description,
                i.Price,
                i.Enabled,
                i.VisibleInPublicMenu,
                i.VisibleInTables,
                i.IsInternalForTables,
                ImageUrl = string.IsNullOrWhiteSpace(i.ImageUrl)
                    ? "/img/default-product.png"
                    : i.ImageUrl
            })
            .ToListAsync();

        return Ok(items);
    }

    // =========================
    // GET PARA MESAS
    // scope = public | hidden | internal
    // =========================
    [HttpGet("items/table-picker")]
    public async Task<IActionResult> GetItemsForTables(
        string companySlug,
        [FromQuery] string scope = "public")
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var query = _db.MenuItems
            .AsNoTracking()
            .Where(i =>
                i.CompanyId == company.Id &&
                !i.IsDeleted &&
                i.Enabled &&
                i.VisibleInTables);

        scope = (scope ?? "").Trim().ToLower();

        query = scope switch
        {
            "hidden" => query.Where(i => !i.IsInternalForTables && !i.VisibleInPublicMenu),
            "internal" => query.Where(i => i.IsInternalForTables),
            _ => query.Where(i => !i.IsInternalForTables && i.VisibleInPublicMenu)
        };

        var items = await query
            .OrderBy(i => i.Category.Name)
            .ThenBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.CategoryId,
                CategoryName = i.Category.Name,
                i.Name,
                i.Description,
                i.Price,
                i.VisibleInPublicMenu,
                i.VisibleInTables,
                i.IsInternalForTables,
                ImageUrl = string.IsNullOrWhiteSpace(i.ImageUrl)
                    ? "/img/default-product.png"
                    : i.ImageUrl
            })
            .ToListAsync();

        return Ok(items);
    }

    // =========================
    // CREATE
    // =========================
    [HttpPost("items")]
    public async Task<IActionResult> Create(
        string companySlug,
        [FromBody] UpsertMenuItemRequestDto req)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var catOk = await _db.Categories
            .AnyAsync(c => c.Id == req.CategoryId && c.CompanyId == company.Id);

        if (!catOk)
            return BadRequest(new { message = "CategoryId inválido" });

        var name = (req.Name ?? "").Trim();
        var desc = (req.Description ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name requerido" });

        var item = new MenuItem
        {
            CompanyId = company.Id,
            CategoryId = req.CategoryId,
            Name = name,
            Description = desc,
            Price = req.Price,
            Enabled = req.Enabled,
            VisibleInPublicMenu = req.VisibleInPublicMenu,
            VisibleInTables = req.VisibleInTables,
            IsInternalForTables = req.IsInternalForTables,
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl)
                ? null
                : req.ImageUrl.Trim()
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new { item.Id });
    }

    // =========================
    // UPDATE
    // =========================
    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> Update(
        string companySlug,
        int id,
        [FromBody] UpsertMenuItemRequestDto req)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                i.CompanyId == company.Id &&
                !i.IsDeleted);

        if (item == null)
            return NotFound("Item not found");

        var catOk = await _db.Categories
            .AnyAsync(c => c.Id == req.CategoryId && c.CompanyId == company.Id);

        if (!catOk)
            return BadRequest(new { message = "CategoryId inválido" });

        var name = (req.Name ?? "").Trim();
        var desc = (req.Description ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name requerido" });

        item.CategoryId = req.CategoryId;
        item.Name = name;
        item.Description = desc;
        item.Price = req.Price;
        item.Enabled = req.Enabled;
        item.VisibleInPublicMenu = req.VisibleInPublicMenu;
        item.VisibleInTables = req.VisibleInTables;
        item.IsInternalForTables = req.IsInternalForTables;
        item.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl)
            ? null
            : req.ImageUrl.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =========================
    // DELETE
    // =========================
    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> Delete(string companySlug, int id)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                i.CompanyId == company.Id &&
                !i.IsDeleted);

        if (item == null)
            return NotFound("Item not found");

        var usedInOrders = await _db.OrderItems
            .AnyAsync(oi => oi.MenuItemId == id);

        if (usedInOrders)
            return Conflict(new
            {
                message = "No se puede eliminar: el producto está en pedidos."
            });

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

//    [HttpGet("items/table-picker")]
//    public async Task<IActionResult> GetItemsForTablePicker(
//[FromRoute] string companySlug,
//[FromQuery] string scope = "public")
//    {
//        var company = await _db.Companies
//            .AsNoTracking()
//            .FirstOrDefaultAsync(c => c.Slug == companySlug);

//        if (company == null)
//            return NotFound("Company not found");

//        var query = _db.MenuItems
//            .AsNoTracking()
//            .Where(i =>
//                i.CompanyId == company.Id &&
//                i.Enabled &&
//                !i.IsDeleted &&
//                i.VisibleInTables &&
//                !i.IsInternalForTables
//            )
//            .Join(_db.Categories.AsNoTracking(),
//                i => i.CategoryId,
//                c => c.Id,
//                (i, c) => new { i, c })
//            .Where(x => x.c.CompanyId == company.Id && x.c.Enabled);

//        scope = (scope ?? "").Trim().ToLowerInvariant();

//        if (scope == "hidden")
//        {
//            query = query.Where(x => !x.i.VisibleInPublicMenu);
//        }
//        else
//        {
//            query = query.Where(x => x.i.VisibleInPublicMenu);
//        }

//        var items = await query
//            .OrderBy(x => x.c.SortOrder)
//            .ThenBy(x => x.i.Name)
//            .Select(x => new
//            {
//                x.i.Id,
//                x.i.Name,
//                x.i.Description,
//                x.i.Price,
//                x.i.CategoryId,
//                CategoryName = x.c.Name
//            })
//            .ToListAsync();

//        return Ok(items);
//    }
}
