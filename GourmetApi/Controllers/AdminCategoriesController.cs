using GourmetApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/{companySlug}")]
public class AdminCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminCategoriesController(AppDbContext db) => _db = db;

    [Authorize]
    [HttpGet("categories")]
    public async Task<IActionResult> Get(string companySlug)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        var list = await _db.Categories.AsNoTracking()
            .Where(c => c.CompanyId == company.Id)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new {
                c.Id,
                c.Name,
                c.SortOrder,
                c.Enabled
            })
            .ToListAsync();

        return Ok(list);
    }

    public class CategoryUpsertDto
    {
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
        public bool Enabled { get; set; } = true;
    }

    [Authorize]
    [HttpPost("categories")]
    public async Task<IActionResult> Create(string companySlug, [FromBody] CategoryUpsertDto req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");

        var name = (req.Name ?? "").Trim();

        var nameExists = await _db.Categories.AnyAsync(c =>
            c.CompanyId == company.Id &&
            c.Enabled &&
            c.Name.ToLower() == name.ToLower());

        if (nameExists)
            return Conflict(new { message = "Ya existe una categoría habilitada con ese nombre." });

        var orderExists = await _db.Categories.AnyAsync(c =>
            c.CompanyId == company.Id &&
            c.Enabled &&
            c.SortOrder == req.SortOrder);

        if (orderExists)
            return Conflict(new { message = "Ya existe una categoría habilitada con ese orden." });

        var entity = new GourmetApi.Entities.Category
        {
            CompanyId = company.Id,
            Name = req.Name.Trim(),
            SortOrder = req.SortOrder,
            Enabled = req.Enabled
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { entity.Id });
    }

    [Authorize]
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> Update(string companySlug, int id, [FromBody] CategoryUpsertDto req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");

        var name = (req.Name ?? "").Trim();

        var nameExists = await _db.Categories.AnyAsync(c =>
            c.CompanyId == company.Id &&
            c.Enabled &&
            c.Id != id &&
            c.Name.ToLower() == name.ToLower());

        if (nameExists)
            return Conflict(new { message = "Ya existe una categoría habilitada con ese nombre." });

        var orderExists = await _db.Categories.AnyAsync(c =>
            c.CompanyId == company.Id &&
            c.Enabled &&
            c.Id != id &&
            c.SortOrder == req.SortOrder);

        if (orderExists)
            return Conflict(new { message = "Ya existe una categoría habilitada con ese orden." });

        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == company.Id);
        if (cat == null) return NotFound("Category not found");

        cat.Name = req.Name.Trim();
        cat.SortOrder = req.SortOrder;
        cat.Enabled = req.Enabled;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(string companySlug, int id)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == company.Id);

        if (category == null)
            return NotFound("Category not found");

        var hasActiveItems = await _db.MenuItems
            .AnyAsync(i =>
                i.CompanyId == company.Id &&
                i.CategoryId == id &&
                i.Enabled &&
                !i.IsDeleted);

        if (hasActiveItems)
        {
            return Conflict(new
            {
                message = "No se puede eliminar la categoría porque tiene productos activos."
            });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}