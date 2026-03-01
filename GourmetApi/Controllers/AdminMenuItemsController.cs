using GourmetApi.Data;
using GourmetApi.Dtos;
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
    [Authorize]
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
                i.Description, // ✅ ahora lo devolvemos también
                i.Price,
                i.Enabled,

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
    [Authorize]
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

        // ✅ blindaje
        var name = (req.Name ?? "").Trim();
        var desc = (req.Description ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name requerido" });

        // si querés obligatoria la descripción:
        // if (string.IsNullOrWhiteSpace(desc))
        //     return BadRequest(new { message = "Description requerida" });

        var item = new Entities.MenuItem
        {
            CompanyId = company.Id,
            CategoryId = req.CategoryId,
            Name = name,
            Description = desc, // ✅ CLAVE
            Price = req.Price,
            Enabled = req.Enabled,

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
    [Authorize]
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
        item.Description = desc; // ✅ CLAVE
        item.Price = req.Price;
        item.Enabled = req.Enabled;

        item.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl)
            ? null
            : req.ImageUrl.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =========================
    // DELETE
    // =========================
    [Authorize]
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
}