using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/{companySlug}/table-sessions")]
public class AdminTableSessionItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminTableSessionItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("{sessionId:int}/items/menu-item")]
    public async Task<IActionResult> AddMenuItem(
        [FromRoute] string companySlug,
        [FromRoute] int sessionId,
        [FromBody] TableSessionAddMenuItemRequestDto req)
    {
        if (req.MenuItemId <= 0)
            return BadRequest("MenuItemId inválido");

        if (req.Qty <= 0)
            return BadRequest("Qty inválido");

        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var session = await _db.TableSessions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.CompanyId == company.Id &&
                x.ClosedAt == null);

        if (session == null)
            return NotFound("Sesión no encontrada");

        var menuItem = await _db.MenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == req.MenuItemId &&
                x.CompanyId == company.Id &&
                x.Enabled &&
                !x.IsDeleted &&
                x.VisibleInTables);

        if (menuItem == null)
            return BadRequest("Producto inválido");

        var lineTotal = menuItem.Price * req.Qty;

        var item = new TableSessionItem
        {
            TableSessionId = session.Id,
            MenuItemId = menuItem.Id,
            Name = menuItem.Name,
            Qty = req.Qty,
            UnitPrice = menuItem.Price,
            LineTotal = lineTotal,
            Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
            IsManual = false,
            IsInternalProduct = menuItem.IsInternalForTables,
            IsDiscount = false
        };

        _db.TableSessionItems.Add(item);

        session.Total += lineTotal;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            item.Id,
            sessionId = session.Id,
            total = session.Total
        });
    }

    [HttpPost("{sessionId:int}/items/manual")]
    public async Task<IActionResult> AddManual(
        [FromRoute] string companySlug,
        [FromRoute] int sessionId,
        [FromBody] AddManualTableItemRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Nombre requerido");

        if (req.Qty <= 0)
            return BadRequest("Qty inválido");

        if (req.UnitPrice < 0)
            return BadRequest("Precio inválido");

        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var session = await _db.TableSessions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.CompanyId == company.Id &&
                x.ClosedAt == null);

        if (session == null)
            return NotFound("Sesión no encontrada");

        var lineTotal = req.UnitPrice * req.Qty;

        var item = new TableSessionItem
        {
            TableSessionId = session.Id,
            Name = req.Name.Trim(),
            Qty = req.Qty,
            UnitPrice = req.UnitPrice,
            LineTotal = lineTotal,
            Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
            IsManual = true,
            IsInternalProduct = false,
            IsDiscount = false
        };

        _db.TableSessionItems.Add(item);

        session.Total += lineTotal;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            item.Id,
            sessionId = session.Id,
            total = session.Total
        });
    }
    [HttpPost("{sessionId:int}/generate-order")]
    public async Task<IActionResult> GenerateOrderFromTable(
     [FromRoute] string companySlug,
     [FromRoute] int sessionId)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var session = await _db.TableSessions
            .Include(x => x.RestaurantTable)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.CompanyId == company.Id &&
                x.ClosedAt == null);

        if (session == null)
            return NotFound("Sesión no encontrada");

        var pendingItems = session.Items
            .Where(x =>
                !x.SentToKitchen &&
                !x.IsDiscount &&
                x.Qty > 0)
            .ToList();

        if (!pendingItems.Any())
            return BadRequest("No hay consumos pendientes para generar pedido");

        var today = DateTime.UtcNow.Date;

        var countToday = await _db.Orders.CountAsync(o =>
            o.CompanyId == company.Id &&
            o.CreatedAt.Date == today);

        var orderNumber = $"A-{(countToday + 1).ToString("0000")}";

        var tableName = session.RestaurantTable?.Name;
        if (string.IsNullOrWhiteSpace(tableName))
            tableName = $"Mesa {session.RestaurantTableId}";

        var now = DateTime.UtcNow;

        var order = new Order
        {
            CompanyId = company.Id,
            CustomerName = tableName,
            Address = "",
            PaymentMethod = session.PaymentMethod ?? "",
            OrderNumber = orderNumber,
            CreatedAt = now,
            Status = OrderStatus.Preparing,
            IsTableOrder = true,
            TableSessionId = session.Id,
            RestaurantTableId = session.RestaurantTableId,
            TableName = tableName,
            Total = pendingItems.Sum(x => x.LineTotal)
        };

        foreach (var it in pendingItems)
        {
            order.Items.Add(new OrderItem
            {
                MenuItemId = it.MenuItemId ?? 0,
                Qty = it.Qty,
                Name = it.Name,
                UnitPrice = it.UnitPrice,
                LineTotal = it.LineTotal,
                Note = it.Note
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var it in pendingItems)
        {
            it.SentToKitchen = true;
            it.SentToKitchenAt = now;
            it.OrderId = order.Id;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            tableName = order.TableName,
            itemsCount = pendingItems.Count
        });
    }

    [HttpPost("{sessionId:int}/items/discount")]
    public async Task<IActionResult> AddDiscount(
        [FromRoute] string companySlug,
        [FromRoute] int sessionId,
        [FromBody] TableSessionAddDiscountRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Motivo requerido");

        if (req.Amount <= 0)
            return BadRequest("Importe inválido");

        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var session = await _db.TableSessions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.CompanyId == company.Id &&
                x.ClosedAt == null);

        if (session == null)
            return NotFound("Sesión no encontrada");

        var item = new TableSessionItem
        {
            TableSessionId = session.Id,
            Name = req.Name.Trim(),
            Qty = 1,
            UnitPrice = -req.Amount,
            LineTotal = -req.Amount,
            Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
            IsManual = true,
            IsInternalProduct = false,
            IsDiscount = true
        };

        _db.TableSessionItems.Add(item);

        session.Total += item.LineTotal;
        if (session.Total < 0)
            session.Total = 0;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            item.Id,
            sessionId = session.Id,
            total = session.Total
        });
    }

    [HttpGet("{sessionId:int}/kitchen-notifications")]
    public async Task<IActionResult> GetKitchenNotifications(
    [FromRoute] string companySlug,
    [FromRoute] int sessionId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var orders = await _db.Orders
            .Where(x =>
                x.CompanyId == company.Id &&
                x.IsTableOrder &&
                x.TableSessionId == sessionId &&
                !x.WaiterNotified &&
                x.Status == OrderStatus.Finished)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.TableName,
                message = $"Pedido de {x.TableName} terminado"
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost("kitchen-notifications/{orderId:int}/read")]
    public async Task<IActionResult> MarkKitchenNotificationRead(
    [FromRoute] string companySlug,
    [FromRoute] int orderId)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.Slug == companySlug);

        if (company == null)
            return NotFound("Company not found");

        var order = await _db.Orders
            .FirstOrDefaultAsync(x =>
                x.Id == orderId &&
                x.CompanyId == company.Id &&
                x.IsTableOrder);

        if (order == null)
            return NotFound("Pedido no encontrado");

        order.WaiterNotified = true;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}