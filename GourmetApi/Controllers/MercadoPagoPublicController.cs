using System.Text.Json;
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

            if (menuItems.Count != requestedIds.Distinct().Count())
            {
                return BadRequest("Hay productos inválidos o deshabilitados.");
            }

            var subtotalBase = request.Items.Sum(i =>
            {
                var item = menuItems.First(x => x.Id == i.MenuItemId);
                return item.Price * i.Qty;
            });

            var pricing = _orderPricingService.Calculate(company, subtotalBase, "MercadoPago");

            var payloadJson = JsonSerializer.Serialize(request);

            var checkout = new MercadoPagoCheckout
            {
                CompanyId = company.Id,
                CustomerName = request.CustomerName,
                Address = request.Address,
                SubtotalBase = pricing.SubtotalBase,
                PaymentSurchargePercent = pricing.PaymentSurchargePercent,
                PaymentSurchargeAmount = pricing.PaymentSurchargeAmount,
                Total = pricing.Total,
                PayloadJson = payloadJson,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.MercadoPagoCheckouts.Add(checkout);
            await _db.SaveChangesAsync();

            var preference = await _mercadoPagoService.CreatePreferenceAsync(checkout, company, companySlug);

            checkout.PreferenceId = preference.Id;
            await _db.SaveChangesAsync();

            return Ok(new CreateMercadoPagoOrderResponse
            {
                OrderId = 0,
                OrderNumber = "",
                InitPoint = preference.InitPoint
            });
        }

        [HttpGet("confirm")]
        public async Task<ActionResult<ConfirmMercadoPagoPaymentResponse>> ConfirmPayment(
            string companySlug,
            [FromQuery] int checkoutId,
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

            var checkout = await _db.MercadoPagoCheckouts
                .FirstOrDefaultAsync(x => x.Id == checkoutId && x.CompanyId == company.Id);

            if (checkout == null)
            {
                return NotFound(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = false,
                    Approved = false,
                    Message = "Checkout no encontrado."
                });
            }

            var storePhone = company.Whatsapp;
            string? whatsappUrl = null;

            if (!string.IsNullOrWhiteSpace(storePhone))
            {
                var orderNumberText = checkout.OrderId.HasValue ? $" de mi pedido #{checkout.OrderId}" : "";
                var text = $"Hola! Ya realicé el pago{orderNumberText}.";
                whatsappUrl = $"https://wa.me/{storePhone}?text={Uri.EscapeDataString(text)}";
            }

            if (string.Equals(checkout.Status, "Approved", StringComparison.OrdinalIgnoreCase) && checkout.OrderId.HasValue)
            {
                var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == checkout.OrderId.Value);

                return Ok(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = true,
                    Approved = true,
                    OrderId = order?.Id ?? 0,
                    OrderNumber = order?.OrderNumber ?? "",
                    Message = "Tu pago fue aprobado y tu pedido fue enviado al local.",
                    StorePhone = storePhone,
                    WhatsappUrl = whatsappUrl
                });
            }

            if (string.Equals(checkout.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(checkout.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new ConfirmMercadoPagoPaymentResponse
                {
                    Ok = true,
                    Approved = false,
                    Message = "El pago no fue aprobado. No se generó ningún pedido."
                });
            }

            return Ok(new ConfirmMercadoPagoPaymentResponse
            {
                Ok = true,
                Approved = false,
                Message = "Todavía estamos confirmando tu pago. Si ya pagaste, aguardá unos segundos y volvé a intentar."
            });
        }
    }
}