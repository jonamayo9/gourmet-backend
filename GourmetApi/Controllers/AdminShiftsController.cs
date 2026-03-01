using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers;

[ApiController]
[Route("api/admin/{companySlug}")]
public class AdminShiftsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminShiftsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(string companySlug)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == companySlug);

        if (company == null) return NotFound("Company not found");

        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.CompanyId == company.Id && s.Enabled)
            .OrderBy(s => s.DayOfWeek ?? -1)
            .ThenBy(s => s.OpenHour)
            .Select(s => new ShiftDto
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                OpenHour = s.OpenHour,
                CloseHour = s.CloseHour,
                Enabled = s.Enabled
            })
            .ToListAsync();

        return Ok(shifts);
    }

    [Authorize]
    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift(string companySlug, [FromBody] ShiftUpsertDto req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        ValidateShift(req);

        var entity = new Shift
        {
            CompanyId = company.Id,
            DayOfWeek = req.DayOfWeek,
            OpenHour = req.OpenHour,
            CloseHour = req.CloseHour,
            Enabled = req.Enabled,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shifts.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { entity.Id });
    }

    [Authorize]
    [HttpPut("shifts/{id:int}")]
    public async Task<IActionResult> UpdateShift(string companySlug, int id, [FromBody] ShiftUpsertDto req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        ValidateShift(req);

        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == company.Id);
        if (shift == null) return NotFound("Shift not found");

        shift.DayOfWeek = req.DayOfWeek;
        shift.OpenHour = req.OpenHour;
        shift.CloseHour = req.CloseHour;
        shift.Enabled = req.Enabled;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("shifts/{id:int}")]
    public async Task<IActionResult> DeleteShift(string companySlug, int id)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
        if (company == null) return NotFound("Company not found");

        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == company.Id);
        if (shift == null) return NotFound("Shift not found");

        _db.Shifts.Remove(shift);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static void ValidateShift(ShiftUpsertDto req)
    {
        if (req.OpenHour < 0 || req.OpenHour > 23) throw new ArgumentException("OpenHour inválido");
        if (req.CloseHour < 0 || req.CloseHour > 23) throw new ArgumentException("CloseHour inválido");
        if (req.OpenHour >= req.CloseHour) throw new ArgumentException("OpenHour debe ser menor a CloseHour");
        if (req.DayOfWeek is < 0 or > 6) throw new ArgumentException("DayOfWeek inválido (0..6) o null");
    }
}