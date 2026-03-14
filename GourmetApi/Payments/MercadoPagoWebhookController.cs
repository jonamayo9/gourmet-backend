using System.Text.Json;
using GourmetApi.Data;
using GourmetApi.Dtos;
using GourmetApi.Entities;
using GourmetApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/payments/mercadopago/webhook")]
    public class MercadoPagoWebhookController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly MercadoPagoService _mercadoPagoService;

        public MercadoPagoWebhookController(AppDbContext db, MercadoPagoService mercadoPagoService)
        {
            _db = db;
            _mercadoPagoService = mercadoPagoService;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            var type = Request.Query["type"].FirstOrDefault();
            var paymentIdRaw = Request.Query["data.id"].FirstOrDefault();
            var companySlug = Request.Query["companySlug"].FirstOrDefault();

            Console.WriteLine("=== WEBHOOK MERCADOPAGO ===");
            Console.WriteLine($"type: {type}");
            Console.WriteLine($"paymentId: {paymentIdRaw}");
            Console.WriteLine($"companySlug: {companySlug}");

            if (!string.Equals(type, "payment", StringComparison.OrdinalIgnoreCase))
                return Ok();

            if (!long.TryParse(paymentIdRaw, out var paymentId))
                return Ok();

            if (string.IsNullOrWhiteSpace(companySlug))
                return Ok();

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Slug == companySlug);
            if (company == null)
                return Ok();

            var payment = await _mercadoPagoService.GetPaymentAsync(paymentId, company);
            if (payment == null)
                return Ok();

            var externalReference = payment.ExternalReference;
            if (string.IsNullOrWhiteSpace(externalReference))
                return Ok();

            if (!int.TryParse(externalReference, out var checkoutId))
                return Ok();

            var checkout = await _db.MercadoPagoCheckouts
                .FirstOrDefaultAsync(x => x.Id == checkoutId && x.CompanyId == company.Id);

            if (checkout == null)
                return Ok();

            checkout.PaymentId = payment.Id.ToString();

            var mpStatus = (payment.Status ?? "").Trim().ToLowerInvariant();

            switch (mpStatus)
            {
                case "approved":
                    checkout.Status = "Approved";

                    if (checkout.OrderId == null)
                    {
                        var request = JsonSerializer.Deserialize<CreateMercadoPagoOrderRequest>(checkout.PayloadJson);

                        if (request == null || request.Items == null || !request.Items.Any())
                        {
                            await _db.SaveChangesAsync();
                            return Ok();
                        }

                        var requestedIds = request.Items.Select(i => i.MenuItemId).ToList();

                        var menuItems = await _db.MenuItems
                            .Where(x => x.CompanyId == company.Id && requestedIds.Contains(x.Id) && x.Enabled)
                            .ToListAsync();

                        if (!menuItems.Any())
                        {
                            await _db.SaveChangesAsync();
                            return Ok();
                        }

                        var order = new Order
                        {
                            CompanyId = company.Id,
                            CustomerName = checkout.CustomerName,
                            Address = checkout.Address,
                            PaymentMethod = "MercadoPago",
                            Status = OrderStatus.Preparing,
                            PaymentStatus = PaymentStatus.Approved,
                            SubtotalBase = checkout.SubtotalBase,
                            PaymentSurchargePercent = checkout.PaymentSurchargePercent,
                            PaymentSurchargeAmount = checkout.PaymentSurchargeAmount,
                            Total = checkout.Total,
                            CreatedAt = DateTime.UtcNow,
                            OrderNumber = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                            PaymentProvider = "MercadoPago",
                            PaymentReference = checkout.PreferenceId,
                            PaidAt = DateTime.UtcNow,
                            LastPaymentId = payment.Id.ToString(),
                            Source = "Public"
                        };

                        foreach (var reqItem in request.Items)
                        {
                            var menuItem = menuItems.FirstOrDefault(x => x.Id == reqItem.MenuItemId);
                            if (menuItem == null)
                                continue;

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

                        checkout.OrderId = order.Id;
                    }

                    break;

                case "rejected":
                    checkout.Status = "Rejected";
                    break;

                case "cancelled":
                case "cancelled_by_user":
                    checkout.Status = "Cancelled";
                    break;

                case "pending":
                case "in_process":
                    checkout.Status = "Pending";
                    break;
            }

            await _db.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost("checkout/{checkoutId:int}/sync-payment")]
        public async Task<IActionResult> SyncPayment(int checkoutId)
        {
            var checkout = await _db.MercadoPagoCheckouts.FirstOrDefaultAsync(o => o.Id == checkoutId);
            if (checkout == null) return NotFound("Checkout not found");

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == checkout.CompanyId);
            if (company == null) return NotFound("Company not found");

            if (string.IsNullOrWhiteSpace(checkout.PaymentId))
                return BadRequest("El checkout no tiene PaymentId para sincronizar.");

            if (!long.TryParse(checkout.PaymentId, out var paymentId))
                return BadRequest("PaymentId inválido.");

            var payment = await _mercadoPagoService.GetPaymentAsync(paymentId, company);
            if (payment == null)
                return BadRequest("No se pudo consultar el pago en Mercado Pago.");

            var mpStatus = (payment.Status ?? "").Trim().ToLowerInvariant();

            switch (mpStatus)
            {
                case "approved":
                    checkout.Status = "Approved";
                    break;

                case "rejected":
                    checkout.Status = "Rejected";
                    break;

                case "cancelled":
                case "cancelled_by_user":
                    checkout.Status = "Cancelled";
                    break;

                case "pending":
                case "in_process":
                    checkout.Status = "Pending";
                    break;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                checkout.Id,
                checkout.Status,
                checkout.PaymentId,
                checkout.OrderId
            });
        }
    }
}