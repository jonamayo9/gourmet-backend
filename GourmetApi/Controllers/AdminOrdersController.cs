using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Dtos.Orders;
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

        [HttpPost("manual")]
        public async Task<IActionResult> CreateManual(string companySlug, [FromBody] AdminCreateOrderDto dto)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("El pedido debe tener al menos un producto.");

            if (dto.PaymentMethod == PaymentMethod.MercadoPago)
                return BadRequest("MercadoPago no está disponible para pedidos creados por gestión interna.");

            var productIds = dto.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _db.MenuItems
                .Where(x => x.CompanyId == company.Id && x.Enabled && productIds.Contains(x.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
                return BadRequest("Uno o más productos no son válidos.");

            var paymentMethod = dto.PaymentMethod switch
            {
                PaymentMethod.Cash => "Cash",
                PaymentMethod.Transfer => "Transfer",
                PaymentMethod.Qr => "Qr",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(paymentMethod))
                return BadRequest("Medio de pago inválido.");

            var paymentStatus = dto.PaymentMethod switch
            {
                PaymentMethod.Cash => PaymentStatus.Approved,
                PaymentMethod.Transfer => PaymentStatus.Pending,
                PaymentMethod.Qr => PaymentStatus.Pending,
                _ => PaymentStatus.None
            };

            var paymentProvider = dto.PaymentMethod switch
            {
                PaymentMethod.Qr => "ManualQr",
                PaymentMethod.Transfer => "ManualTransfer",
                PaymentMethod.Cash => "Cash",
                _ => null
            };

            decimal subtotalBase = 0m;

            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest("La cantidad debe ser mayor a 0.");

                var product = products.First(x => x.Id == item.ProductId);

                var unitPrice = product.Price;
                var lineTotal = unitPrice * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    MenuItemId = product.Id,
                    Qty = item.Quantity,
                    Name = product.Name,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal,
                    Note = item.Notes
                });

                subtotalBase += lineTotal;
            }

            var surchargePercent = dto.PaymentMethod switch
            {
                PaymentMethod.Cash => company.CashSurchargePercent,
                PaymentMethod.Transfer => company.TransferSurchargePercent,
                PaymentMethod.Qr => company.QrSurchargePercent,
                _ => 0m
            };

            var surchargeAmount = Math.Round(
                subtotalBase * surchargePercent / 100m,
                2,
                MidpointRounding.AwayFromZero);

            var total = subtotalBase + surchargeAmount;

            var order = new Order
            {
                CompanyId = company.Id,
                CustomerName = dto.CustomerName?.Trim() ?? "",
                Address = dto.Address?.Trim() ?? "",
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                PaymentProvider = paymentProvider,
                Source = "Admin",
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow,
                PaidAt = paymentStatus == PaymentStatus.Approved ? DateTime.UtcNow : null,

                SubtotalBase = subtotalBase,
                PaymentSurchargePercent = surchargePercent,
                PaymentSurchargeAmount = surchargeAmount,
                Total = total,

                Items = orderItems
            };

            // Si estos campos existen en tu entidad, dejalos.
            // Si NO existen, borralos del bloque.
            // order.Notes = dto.Notes?.Trim();
            // order.OrderType = dto.OrderType;

            order.OrderNumber = await GenerateOrderNumber(company.Id);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.PaymentMethod,
                paymentStatus = order.PaymentStatus.ToString(),
                order.SubtotalBase,
                order.PaymentSurchargePercent,
                order.PaymentSurchargeAmount,
                order.Total,
                order.Source
            });
        }

        private async Task<string> GenerateOrderNumber(int companyId)
            {
                var today = DateTime.UtcNow.Date;

                var countToday = await _db.Orders
                    .CountAsync(x => x.CompanyId == companyId && x.CreatedAt >= today);

                return $"A-{(countToday + 1):0000}";
            }

        [HttpGet("payment-options")]
        public async Task<IActionResult> GetPaymentOptions(string companySlug)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            return Ok(new
            {
                transferEnabled = company.TransferEnabled,
                transferAlias = company.Alias,

                cashSurchargePercent = company.CashSurchargePercent,
                transferSurchargePercent = company.TransferSurchargePercent,
                qrSurchargePercent = company.QrSurchargePercent,
                mercadoPagoSurchargePercent = company.MercadoPagoSurchargePercent
            });
        }
    }
    }