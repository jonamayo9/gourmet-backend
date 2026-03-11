using GourmetApi.Data;
using GourmetApi.Entities;
using GourmetApi.Enums;
using GourmetApi.Models.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/admin/{companySlug}/table-sessions")]
    [Authorize]
    public class AdminTableSessionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminTableSessionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("{sessionId:int}/items/product")]
        public async Task<ActionResult> AddProductToTable(
            [FromRoute] string companySlug,
            [FromRoute] int sessionId,
            [FromBody] AddTableItemProductDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var session = await _db.TableSessions
                .AsNoTracking()
                .Include(x => x.RestaurantTable)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Order)
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.CompanyId == company.Id);

            if (session == null)
                return NotFound("Sesión de mesa no encontrada.");

            if (session.Status == TableSessionStatus.Closed)
                return BadRequest("La mesa ya está cerrada.");

            if (request.Qty <= 0)
                return BadRequest("La cantidad debe ser mayor a 0.");

            var product = await _db.MenuItems
                .FirstOrDefaultAsync(x =>
                    x.Id == request.MenuItemId &&
                    x.CompanyId == company.Id &&
                    x.Enabled &&
                    !x.IsDeleted);

            if (product == null)
                return NotFound("Producto no encontrado.");

            if (!product.VisibleInTables)
                return BadRequest("Este producto no está disponible para mesas.");

            var lineTotal = product.Price * request.Qty;

            var item = new TableSessionItem
            {
                TableSessionId = session.Id,
                MenuItemId = product.Id,
                Name = product.Name,
                Qty = request.Qty,
                UnitPrice = product.Price,
                LineTotal = lineTotal,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                IsManual = false,
                IsInternalProduct = product.IsInternalForTables
            };

            _db.TableSessionItems.Add(item);

            session.Total += lineTotal;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Producto agregado",
                itemId = item.Id,
                total = session.Total
            });
        }

        //[HttpPost("{sessionId:int}/items/manual")]
        //public async Task<ActionResult> AddManualItemToTable(
        //    [FromRoute] string companySlug,
        //    [FromRoute] int sessionId,
        //    [FromBody] AddTableItemManualDto request)
        //{
        //    var company = await _db.Companies
        //        .FirstOrDefaultAsync(x => x.Slug == companySlug);

        //    if (company == null)
        //        return NotFound("Empresa no encontrada.");

        //    var session = await _db.TableSessions
        //        .Include(x => x.Items)
        //        .FirstOrDefaultAsync(x =>
        //            x.Id == sessionId &&
        //            x.CompanyId == company.Id);

        //    if (session == null)
        //        return NotFound("Sesión de mesa no encontrada.");

        //    if (session.Status == TableSessionStatus.Closed)
        //        return BadRequest("La mesa ya está cerrada.");

        //    if (string.IsNullOrWhiteSpace(request.Name))
        //        return BadRequest("El nombre es obligatorio.");

        //    if (request.Qty <= 0)
        //        return BadRequest("La cantidad debe ser mayor a 0.");

        //    if (request.UnitPrice < 0)
        //        return BadRequest("El precio no puede ser negativo.");

        //    var lineTotal = request.UnitPrice * request.Qty;

        //    var item = new TableSessionItem
        //    {
        //        TableSessionId = session.Id,
        //        MenuItemId = null,
        //        Name = request.Name.Trim(),
        //        Qty = request.Qty,
        //        UnitPrice = request.UnitPrice,
        //        LineTotal = lineTotal,
        //        Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        //        IsManual = true,
        //        IsInternalProduct = false
        //    };

        //    _db.TableSessionItems.Add(item);

        //    session.Total += lineTotal;

        //    await _db.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        message = "Consumo manual agregado",
        //        itemId = item.Id,
        //        total = session.Total
        //    });
        //}

        [HttpGet("{sessionId:int}")]
        public async Task<ActionResult<TableSessionDetailDto>> GetSessionDetail(
      [FromRoute] string companySlug,
      [FromRoute] int sessionId)
        {
            try
            {
                var company = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Slug == companySlug);

                if (company == null)
                    return NotFound("Empresa no encontrada.");

                var session = await _db.TableSessions
                    .AsNoTracking()
                    .Include(x => x.RestaurantTable)
                    .Include(x => x.Items)
                        .ThenInclude(i => i.Order)
                    .FirstOrDefaultAsync(x =>
                        x.Id == sessionId &&
                        x.CompanyId == company.Id);

                if (session == null)
                    return NotFound("Sesión de mesa no encontrada.");

                var dto = new TableSessionDetailDto
                {
                    SessionId = session.Id,
                    TableId = session.RestaurantTableId,
                    TableNumber = session.RestaurantTable.Number,
                    TableName = string.IsNullOrWhiteSpace(session.RestaurantTable.Name)
                        ? $"Mesa {session.RestaurantTable.Number}"
                        : session.RestaurantTable.Name,
                    Capacity = session.RestaurantTable.Capacity,
                    Status = session.Status,
                    TotalGuests = session.TotalGuests,
                    Adults = session.Adults,
                    Children = session.Children,
                    Notes = session.Notes,
                    Total = session.Total,
                    PaymentMethod = session.PaymentMethod,
                    PaymentStatus = session.PaymentStatus,
                    OpenedAt = session.OpenedAt,
                    ClosedAt = session.ClosedAt,
                    HasPendingKitchenItems = session.Items.Any(x => !x.IsDiscount && !x.SentToKitchen),

                    Items = session.Items
                        .OrderBy(x => x.Id)
                        .Select(x => new TableSessionItemDto
                        {
                            Id = x.Id,
                            MenuItemId = x.MenuItemId,
                            Name = x.Name,
                            Qty = x.Qty,
                            UnitPrice = x.UnitPrice,
                            LineTotal = x.LineTotal,
                            Note = x.Note,
                            IsManual = x.IsManual,
                            IsInternalProduct = x.IsInternalProduct,
                            IsDiscount = x.IsDiscount,
                            SentToKitchen = x.SentToKitchen,
                            SentToKitchenAt = x.SentToKitchenAt,
                            KitchenStatus = x.Order != null ? x.Order.Status.ToString() : null,
                            IsFinished = x.Order != null && x.Order.Status == OrderStatus.Finished
                        })
                        .ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpPatch("{sessionId:int}/request-bill")]
        public async Task<ActionResult> RequestBill(
    [FromRoute] string companySlug,
    [FromRoute] int sessionId)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var session = await _db.TableSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.CompanyId == company.Id);

            if (session == null)
                return NotFound("Sesión de mesa no encontrada.");

            if (session.Status == TableSessionStatus.Closed)
                return BadRequest("La mesa ya está cerrada.");

            if (session.Status == TableSessionStatus.Paid)
                return BadRequest("La mesa ya fue pagada.");

            if (session.Status == TableSessionStatus.BillRequested)
                return BadRequest("La mesa ya pidió la cuenta.");

            if (session.Status != TableSessionStatus.Open)
                return BadRequest("La mesa no está en un estado válido para pedir la cuenta.");

            session.Status = TableSessionStatus.BillRequested;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "La mesa pasó a estado 'Pidió cuenta'.",
                sessionId = session.Id,
                status = session.Status.ToString()
            });
        }

        [HttpPatch("{sessionId:int}/pay")]
        public async Task<ActionResult> PayTable(
    [FromRoute] string companySlug,
    [FromRoute] int sessionId,
    [FromBody] PayTableSessionDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var session = await _db.TableSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.CompanyId == company.Id);

            if (session == null)
                return NotFound("Sesión de mesa no encontrada.");

            if (session.Status == TableSessionStatus.Closed)
                return BadRequest("La mesa ya está cerrada.");

            if (session.Status == TableSessionStatus.Paid)
                return BadRequest("La mesa ya fue pagada.");

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return BadRequest("El método de pago es obligatorio.");

            session.PaymentMethod = request.PaymentMethod.Trim();
            session.PaymentStatus = string.IsNullOrWhiteSpace(request.PaymentStatus)
                ? "Paid"
                : request.PaymentStatus.Trim();

            session.Status = TableSessionStatus.Paid;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Pago registrado correctamente.",
                sessionId = session.Id,
                total = session.Total,
                paymentMethod = session.PaymentMethod,
                status = session.Status.ToString()
            });
        }

        [HttpPatch("{sessionId:int}/close")]
        public async Task<ActionResult> CloseTable(
    [FromRoute] string companySlug,
    [FromRoute] int sessionId)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var session = await _db.TableSessions
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.CompanyId == company.Id);

            if (session == null)
                return NotFound("Sesión de mesa no encontrada.");

            if (session.Status == TableSessionStatus.Closed)
                return BadRequest("La mesa ya está cerrada.");

            if (session.Status != TableSessionStatus.Paid)
                return BadRequest("La mesa debe estar pagada antes de cerrarse.");

            session.Status = TableSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesa cerrada correctamente.",
                sessionId = session.Id,
                status = session.Status.ToString(),
                closedAt = session.ClosedAt
            });
        }

    //    [HttpPost("{sessionId:int}/generate-order")]
    //    public async Task<IActionResult> GenerateOrderFromTable(
    //[FromRoute] string companySlug,
    //[FromRoute] int sessionId)
    //    {
    //        var company = await _db.Companies
    //            .FirstOrDefaultAsync(x => x.Slug == companySlug);

    //        if (company == null)
    //            return NotFound("Company not found");

    //        var session = await _db.TableSessions
    //            .Include(x => x.RestaurantTable)
    //            .Include(x => x.Items)
    //            .FirstOrDefaultAsync(x =>
    //                x.Id == sessionId &&
    //                x.CompanyId == company.Id &&
    //                x.ClosedAt == null);

    //        if (session == null)
    //            return NotFound("Sesión no encontrada");

    //        var pendingItems = session.Items
    //            .Where(x => !x.SentToKitchen && !x.IsDiscount)
    //            .ToList();

    //        if (!pendingItems.Any())
    //            return BadRequest("No hay consumos pendientes para generar pedido");

    //        var today = DateTime.UtcNow.Date;

    //        var countToday = await _db.Orders.CountAsync(o =>
    //            o.CompanyId == company.Id &&
    //            o.CreatedAt.Date == today);

    //        var orderNumber = $"A-{(countToday + 1).ToString("0000")}";

    //        var tableName = session.RestaurantTable?.Name;
    //        if (string.IsNullOrWhiteSpace(tableName))
    //            tableName = $"Mesa {session.RestaurantTableId}";

    //        var order = new Order
    //        {
    //            CompanyId = company.Id,
    //            CustomerName = tableName,
    //            Address = "",
    //            PaymentMethod = session.PaymentMethod ?? "",
    //            OrderNumber = orderNumber,
    //            CreatedAt = DateTime.UtcNow,
    //            Status = OrderStatus.Preparing,
    //            IsTableOrder = true,
    //            TableSessionId = session.Id,
    //            RestaurantTableId = session.RestaurantTableId,
    //            TableName = tableName
    //        };

    //        decimal total = 0;

    //        foreach (var it in pendingItems)
    //        {
    //            order.Items.Add(new OrderItem
    //            {
    //                MenuItemId = it.MenuItemId,
    //                Qty = it.Qty,
    //                Name = it.Name,
    //                UnitPrice = it.UnitPrice,
    //                LineTotal = it.LineTotal,
    //                Note = it.Note
    //            });

    //            total += it.LineTotal;
    //        }

    //        order.Total = total;

    //        _db.Orders.Add(order);
    //        await _db.SaveChangesAsync();

    //        foreach (var it in pendingItems)
    //        {
    //            it.SentToKitchen = true;
    //            it.SentToKitchenAt = DateTime.UtcNow;
    //            it.Order.Id = order.Id;
    //        }

    //        await _db.SaveChangesAsync();

    //        return Ok(new
    //        {
    //            orderId = order.Id,
    //            orderNumber = order.OrderNumber,
    //            tableName = order.TableName,
    //            itemsCount = pendingItems.Count
    //        });
    //    }
    }
}