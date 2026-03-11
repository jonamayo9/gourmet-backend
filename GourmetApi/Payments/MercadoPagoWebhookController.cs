using GourmetApi.Data;
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

            if (!int.TryParse(externalReference, out var orderId))
                return Ok();

            var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.CompanyId == company.Id);
            if (order == null)
                return Ok();

            order.PaymentProvider = "MercadoPago";
            order.LastPaymentId = payment.Id.ToString();

            var mpStatus = (payment.Status ?? "").Trim().ToLowerInvariant();

            switch (mpStatus)
            {
                case "approved":
                    order.PaymentStatus = PaymentStatus.Approved;

                    if (order.PaidAt == null)
                        order.PaidAt = DateTime.UtcNow;

                    if (order.Status == OrderStatus.New)
                        order.Status = OrderStatus.Preparing;

                    break;

                case "rejected":
                    order.PaymentStatus = PaymentStatus.Rejected;
                    break;

                case "cancelled":
                case "cancelled_by_user":
                    order.PaymentStatus = PaymentStatus.Cancelled;
                    break;

                case "pending":
                case "in_process":
                    order.PaymentStatus = PaymentStatus.Pending;
                    break;
            }

            await _db.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost("orders/{orderId:int}/sync-payment")]
        public async Task<IActionResult> SyncPayment(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound("Order not found");

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == order.CompanyId);
            if (company == null) return NotFound("Company not found");

            if (order.PaymentMethod != "MercadoPago")
                return BadRequest("El pedido no corresponde a Mercado Pago.");

            if (string.IsNullOrWhiteSpace(order.LastPaymentId))
                return BadRequest("El pedido no tiene LastPaymentId para sincronizar.");

            if (!long.TryParse(order.LastPaymentId, out var paymentId))
                return BadRequest("LastPaymentId inválido.");

            var payment = await _mercadoPagoService.GetPaymentAsync(paymentId, company);
            if (payment == null)
                return BadRequest("No se pudo consultar el pago en Mercado Pago.");

            var mpStatus = (payment.Status ?? "").Trim().ToLowerInvariant();

            order.PaymentProvider = "MercadoPago";
            order.LastPaymentId = payment.Id.ToString();

            switch (mpStatus)
            {
                case "approved":
                    order.PaymentStatus = PaymentStatus.Approved;

                    if (order.PaidAt == null)
                        order.PaidAt = DateTime.UtcNow;

                    if (order.Status == OrderStatus.New)
                        order.Status = OrderStatus.Preparing;

                    break;

                case "rejected":
                    order.PaymentStatus = PaymentStatus.Rejected;
                    break;

                case "cancelled":
                case "cancelled_by_user":
                    order.PaymentStatus = PaymentStatus.Cancelled;
                    break;

                case "pending":
                case "in_process":
                    order.PaymentStatus = PaymentStatus.Pending;
                    break;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                paymentStatus = order.PaymentStatus.ToString(),
                order.PaidAt,
                order.LastPaymentId
            });
        }
    }
}