using global::GourmetApi.Data;
using global::GourmetApi.Dtos;
using global::GourmetApi.Services;
using GourmetApi.Entities;
using GourmetApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/public/{companySlug}/payments/mercadopago")]
    public class MercadoPagoPublicController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly OrderPricingService _orderPricingService;

        public MercadoPagoPublicController(
            AppDbContext db,
            MercadoPagoService mercadoPagoService,
            OrderPricingService orderPricingService)
        {
            _db = db;
            _mercadoPagoService = mercadoPagoService;
            _orderPricingService = orderPricingService;
        }

        [HttpPost]
        public async Task<ActionResult<CreateMercadoPagoOrderResponse>> CreatePayment(
            string companySlug,
            [FromBody] CreateMercadoPagoOrderRequest request)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Slug == companySlug);
            if (company == null)
            {
                return NotFound("Empresa no encontrada.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest("No hay productos.");
            }

            var requestedIds = request.Items.Select(i => i.MenuItemId).ToList();

            var menuItems = await _db.MenuItems
                .Where(x => x.CompanyId == company.Id && requestedIds.Contains(x.Id) && x.Enabled)
                .ToListAsync();

            if (!menuItems.Any())
            {
                return BadRequest("Productos inválidos.");
            }

            var subtotalBase = request.Items.Sum(i =>
            {
                var item = menuItems.First(x => x.Id == i.MenuItemId);
                return item.Price * i.Qty;
            });

            var pricing = _orderPricingService.Calculate(company, subtotalBase, "MercadoPago");

            var order = new Order
            {
                CompanyId = company.Id,
                CustomerName = request.CustomerName,
                Address = request.Address,
                PaymentMethod = "MercadoPago",
                Status = OrderStatus.New,
                PaymentStatus = PaymentStatus.Pending,
                SubtotalBase = pricing.SubtotalBase,
                PaymentSurchargePercent = pricing.PaymentSurchargePercent,
                PaymentSurchargeAmount = pricing.PaymentSurchargeAmount,
                Total = pricing.Total,
                CreatedAt = DateTime.UtcNow,
                OrderNumber = Guid.NewGuid().ToString("N")[..8].ToUpper()
            };

            foreach (var reqItem in request.Items)
            {
                var menuItem = menuItems.First(x => x.Id == reqItem.MenuItemId);
                var lineTotal = menuItem.Price * reqItem.Qty;

                order.Items.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    UnitPrice = menuItem.Price,
                    Qty = reqItem.Qty,
                    LineTotal = lineTotal,
                    Note = reqItem.Note
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var preference = await _mercadoPagoService.CreatePreferenceAsync(order, company, companySlug);

            order.PaymentProvider = "MercadoPago";
            order.PaymentReference = preference.Id;

            await _db.SaveChangesAsync();

            return Ok(new CreateMercadoPagoOrderResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                InitPoint = preference.InitPoint
            });
        }

        [HttpPost("retry/{orderId:int}")]
        public async Task<ActionResult<CreateMercadoPagoOrderResponse>> RetryPayment(
            string companySlug,
            int orderId)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Slug == companySlug);
            if (company == null)
            {
                return NotFound("Empresa no encontrada.");
            }

            var order = await _db.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId && x.CompanyId == company.Id);

            if (order == null)
            {
                return NotFound("Pedido no encontrado.");
            }

            if (order.PaymentMethod != "MercadoPago")
            {
                return BadRequest("El pedido no corresponde a Mercado Pago.");
            }

            if (order.PaymentStatus != PaymentStatus.Pending &&
                order.PaymentStatus != PaymentStatus.Rejected)
            {
                return BadRequest("El pedido no está disponible para reintentar pago.");
            }

            if (order.Status == OrderStatus.Canceled || order.Status == OrderStatus.Delivered)
            {
                return BadRequest("El pedido no se puede volver a pagar.");
            }

            var preference = await _mercadoPagoService.CreatePreferenceAsync(order, company, companySlug);

            order.PaymentProvider = "MercadoPago";
            order.PaymentReference = preference.Id;
            order.PaymentStatus = PaymentStatus.Pending;
            order.LastPaymentId = null;
            order.PaidAt = null;

            await _db.SaveChangesAsync();

            return Ok(new CreateMercadoPagoOrderResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                InitPoint = preference.InitPoint
            });
        }

        [HttpGet("confirm")]
        public async Task<ActionResult<ConfirmMercadoPagoPaymentResponse>> ConfirmPayment(
            string companySlug,
            [FromQuery] int orderId,
            [FromQuery] string? paymentId)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Slug == companySlug);
            if (company == null)
            {
                return NotFound(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = false,
                    Approved = false,
                    Message = "Empresa no encontrada."
                });
            }

            var order = await _db.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId && x.CompanyId == company.Id);

            if (order == null)
            {
                return NotFound(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = false,
                    Approved = false,
                    Message = "Pedido no encontrado."
                });
            }

            var storePhone = company.Whatsapp;
            string? whatsappUrl = null;

            if (!string.IsNullOrWhiteSpace(storePhone))
            {
                var text = $"Hola! Ya realicé el pago de mi pedido {order.OrderNumber}.";
                whatsappUrl = $"https://wa.me/{storePhone}?text={Uri.EscapeDataString(text)}";
            }

            if (order.PaymentStatus == PaymentStatus.Approved)
            {
                if (!string.IsNullOrWhiteSpace(paymentId) &&
                    !string.IsNullOrWhiteSpace(order.LastPaymentId) &&
                    order.LastPaymentId != paymentId)
                {
                    return BadRequest(new ConfirmMercadoPagoPaymentResponse
                    {
                        Ok = false,
                        Approved = false,
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber,
                        Message = "El pago informado no coincide con el pedido."
                    });
                }

                return Ok(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = true,
                    Approved = true,
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Message = "Tu pago fue aprobado y tu pedido fue enviado al local.",
                    StorePhone = storePhone,
                    WhatsappUrl = whatsappUrl
                });
            }

            return Ok(new ConfirmMercadoPagoPaymentResponse
            {
                Ok = true,
                Approved = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Message = "Todavía estamos confirmando tu pago. Si ya pagaste, aguardá unos segundos y volvé a intentar."
            });
        }
    }
}