using GourmetApi.Entities;

namespace GourmetApi.Services
{
    public class OrderPricingResult
    {
        public decimal SubtotalBase { get; set; }
        public decimal PaymentSurchargePercent { get; set; }
        public decimal PaymentSurchargeAmount { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderPricingService
    {
        public OrderPricingResult Calculate(Company company, decimal subtotalBase, string? paymentMethod)
        {
            if (subtotalBase < 0)
            {
                subtotalBase = 0;
            }

            var result = new OrderPricingResult
            {
                SubtotalBase = subtotalBase,
                PaymentSurchargePercent = 0m,
                PaymentSurchargeAmount = 0m,
                Total = subtotalBase
            };

            var method = (paymentMethod ?? "").Trim().ToLowerInvariant();

            if (method == "transfer" &&
                company.TransferSurchargeEnabled &&
                company.TransferSurchargePercent > 0)
            {
                result.PaymentSurchargePercent = company.TransferSurchargePercent;
                result.PaymentSurchargeAmount = Math.Round(
                    subtotalBase * company.TransferSurchargePercent / 100m, 2);
            }
            else if (method == "mercadopago" &&
                     company.MercadoPagoSurchargeEnabled &&
                     company.MercadoPagoSurchargePercent > 0)
            {
                result.PaymentSurchargePercent = company.MercadoPagoSurchargePercent;
                result.PaymentSurchargeAmount = Math.Round(
                    subtotalBase * company.MercadoPagoSurchargePercent / 100m, 2);
            }

            result.Total = Math.Round(subtotalBase + result.PaymentSurchargeAmount, 2);

            return result;
        }
    }
}