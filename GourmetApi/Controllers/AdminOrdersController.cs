using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/admin/{companySlug}")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminOrdersController(AppDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
     string companySlug,
     [FromQuery] DateOnly? date,
     [FromQuery] OrderStatus? status)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.Slug == companySlug);

            if (company == null)
                return NotFound("Company not found");

            var q = _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == company.Id);

            if (date.HasValue)
            {
                var targetDate = date.Value;

                var start = DateTime.SpecifyKind(
                    targetDate.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);

                var end = DateTime.SpecifyKind(
                    targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);

                q = q.Where(o => o.CreatedAt >= start && o.CreatedAt < end);
            }

            if (status.HasValue)
                q = q.Where(o => o.Status == status.Value);

            var orders = await q
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.CreatedAt,
                    o.Total,
                    status = (int)o.Status,

                    o.CustomerName,
                    o.Address,

                    o.PaymentMethod,
                    paymentStatus = o.PaymentStatus,

                    o.IsTableOrder,
                    o.TableName,
                    o.TableSessionId,
                    o.RestaurantTableId
                })
                .ToListAsync();

            return Ok(orders);
        }

        [Authorize]
        [HttpPatch("orders/{orderId:int}/status")]
        public async Task<IActionResult> UpdateStatus(string companySlug, int orderId, [FromBody] UpdateOrderStatusRequestDto req)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
            if (company == null)
                return NotFound("Company not found");

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == company.Id);
            if (order == null)
                return NotFound("Order not found");

            var newStatus = req.Status;

            // Si viene "Enviado" desde cocina y es pedido de mesa,
            // en realidad pasa a Finalizado
            if (order.IsTableOrder && req.Status == OrderStatus.Delivered)
            {
                newStatus = OrderStatus.Finished;
            }

            order.Status = newStatus;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpGet("orders/{orderId:int}")]
        public async Task<IActionResult> GetOrderDetail(string companySlug, int orderId)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(c => c.Slug == companySlug);

            if (company == null)
                return NotFound("Company not found");

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == company.Id);

            if (order == null)
                return NotFound("Order not found");

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.CreatedAt,
                order.Total,
                status = (int)order.Status,

                order.CustomerName,
                order.Address,

                order.PaymentMethod,
                paymentStatus = order.PaymentStatus,

                order.IsTableOrder,
                order.TableName,
                order.TableSessionId,
                order.RestaurantTableId,

                items = order.Items.Select(i => new
                {
                    i.Id,
                    i.MenuItemId,
                    qty = i.Qty,
                    i.Name,
                    i.UnitPrice,
                    i.LineTotal,
                    i.Note
                }).ToList()
            });
        }
    }
}