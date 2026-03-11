namespace GourmetApi.Services
{
    using GourmetApi.Entities;
    using GourmetApi.Models;
    using MercadoPago.Client.Payment;
    using MercadoPago.Client.Preference;
    using MercadoPago.Config;
    using MercadoPago.Resource.Payment;
    using MercadoPago.Resource.Preference;
    using Microsoft.Extensions.Options;

    public class MercadoPagoService
    {
        private readonly MercadoPagoOptions _options;

        public MercadoPagoService(IOptions<MercadoPagoOptions> options)
        {
            _options = options.Value;
        }

        public async Task<Preference> CreatePreferenceAsync(
            Order order,
            Company company,
            string companySlug)
        {
            if (company == null)
                throw new Exception("Empresa no encontrada.");

            if (!company.MercadoPagoEnabled)
                throw new Exception("Mercado Pago no está habilitado para esta empresa.");

            if (string.IsNullOrWhiteSpace(company.MercadoPagoAccessToken))
                throw new Exception("La empresa no tiene configurado el Access Token de Mercado Pago.");

            MercadoPagoConfig.AccessToken = company.MercadoPagoAccessToken;

            var client = new PreferenceClient();

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Pedido {company.Name} #{order.OrderNumber}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = Convert.ToDecimal(order.Total)
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = _options.SuccessUrl,
                    Failure = _options.FailureUrl,
                    Pending = _options.PendingUrl
                },
                AutoReturn = "approved",
                NotificationUrl = $"{_options.WebhookUrl}?companySlug={companySlug}",
                ExternalReference = order.Id.ToString(),
                StatementDescriptor = company.Name.Length > 13
                    ? company.Name.Substring(0, 13)
                    : company.Name
            };

            return await client.CreateAsync(request);
        }

        public async Task<Payment> GetPaymentAsync(long paymentId, Company company)
        {
            if (company == null)
                throw new Exception("Empresa no encontrada.");

            if (string.IsNullOrWhiteSpace(company.MercadoPagoAccessToken))
                throw new Exception("La empresa no tiene configurado el Access Token de Mercado Pago.");

            MercadoPagoConfig.AccessToken = company.MercadoPagoAccessToken;

            var client = new PaymentClient();
            return await client.GetAsync(paymentId);
        }
    }
}