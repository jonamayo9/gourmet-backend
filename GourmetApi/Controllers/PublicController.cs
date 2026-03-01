using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GourmetApi.Data;
using GourmetApi.Models;

namespace GourmetApi.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public PublicController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{companySlug}/menu")]
    public async Task<IActionResult> GetMenu([FromRoute] string companySlug)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound(new { message = "Empresa no encontrada" });

        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.CompanyId == company.Id && s.Enabled)
            .OrderBy(s => s.OpenHour)
            .Select(s => new
            {
                dia = s.DayOfWeek,
                abre = s.OpenHour,
                cierra = s.CloseHour
            })
            .ToListAsync();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.CompanyId == company.Id && c.Enabled)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Name)
            .ToListAsync();

        var menu = await _db.MenuItems.AsNoTracking()
            .Where(i => i.CompanyId == company.Id && i.Enabled && !i.IsDeleted)
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
                categoria = x.c.Name,
                nombre = x.i.Name,
                precio = x.i.Price,

                // ✅ imagen real del item (fallback al logo si no tiene)
                img = string.IsNullOrWhiteSpace(x.i.ImageUrl) ? company.LogoUrl : x.i.ImageUrl
            })
            .ToListAsync();

        var dto = new
        {
            negocio = new
            {
                nombre = company.Name,
                logo = company.LogoUrl,
                telefono = company.Whatsapp,
                alias = company.Alias,
                turnos = shifts
            },
            categorias = categories,
            menu = menu
        };

        return Ok(dto);
    }

    [HttpPost("{companySlug}/orders")]
    public async Task<IActionResult> CreateOrder(
    [FromRoute] string companySlug,
    [FromBody] OrderCreateRequestDto req)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Slug == companySlug);
        if (company == null)
            return NotFound(new { message = "Empresa no encontrada" });

        if (string.IsNullOrWhiteSpace(req.CustomerName) ||
            string.IsNullOrWhiteSpace(req.Address))
            return BadRequest(new { message = "Faltan datos del cliente" });

        if (req.Items == null || req.Items.Count == 0)
            return BadRequest(new { message = "El pedido no tiene items" });

        if (req.Items.Any(i => i.Qty <= 0))
            return BadRequest(new { message = "Items inválidos" });

        var ids = req.Items.Select(x => x.MenuItemId).Distinct().ToList();

        var products = await _db.MenuItems.AsNoTracking()
            .Where(x => x.CompanyId == company.Id &&
                        x.Enabled &&
                        !x.IsDeleted &&
                        ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Price })
            .ToListAsync();

        if (products.Count != ids.Count)
            return BadRequest(new { message = "Hay productos inválidos o deshabilitados" });

        var today = DateTime.UtcNow.Date;

        var countToday = await _db.Orders.CountAsync(o =>
            o.CompanyId == company.Id &&
            o.CreatedAt.Date == today);

        var orderNumber = $"A-{(countToday + 1).ToString("0000")}";

        var order = new GourmetApi.Entities.Order
        {
            CompanyId = company.Id,
            CustomerName = req.CustomerName.Trim(),
            Address = req.Address.Trim(),
            PaymentMethod = req.PaymentMethod?.Trim() ?? "",
            OrderNumber = orderNumber,
            CreatedAt = DateTime.UtcNow
        };

        decimal total = 0;

        foreach (var it in req.Items)
        {
            var p = products.First(x => x.Id == it.MenuItemId);

            var lineTotal = p.Price * it.Qty;
            total += lineTotal;

            order.Items.Add(new GourmetApi.Entities.OrderItem
            {
                MenuItemId = p.Id,
                Qty = it.Qty,
                Name = p.Name,
                UnitPrice = p.Price,
                LineTotal = lineTotal,
                Note = it.Note
            });
        }

        order.Total = total;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            createdAt = order.CreatedAt
        });
    }
}