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
            MercadoPagoCheckout checkout,
            Company company,
            string companySlug)
        {
            if (company == null)
                throw new Exception("Empresa no encontrada.");

            if (!company.MercadoPagoEnabled)
                throw new Exception("Mercado Pago no está habilitado para esta empresa.");

            if (string.IsNullOrWhiteSpace(company.MercadoPagoAccessToken))
                throw new Exception("La empresa no tiene configurado el Access Token de Mercado Pago.");

            if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
                throw new Exception("No está configurada la URL base del frontend.");

            if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
                throw new Exception("No está configurada la URL del webhook.");

            MercadoPagoConfig.AccessToken = company.MercadoPagoAccessToken;

            var client = new PreferenceClient();

            var baseFrontUrl = _options.FrontendBaseUrl.TrimEnd('/');
            var encodedCompanySlug = Uri.EscapeDataString(companySlug);

            var successUrl = $"{baseFrontUrl}/payment/pago-exito.html?company={encodedCompanySlug}";
            var failureUrl = $"{baseFrontUrl}/payment/pago-error.html?company={encodedCompanySlug}";
            var pendingUrl = $"{baseFrontUrl}/payment/pago-pendiente.html?company={encodedCompanySlug}";

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Pedido {company.Name}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = Convert.ToDecimal(checkout.Total)
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = successUrl,
                    Failure = failureUrl,
                    Pending = pendingUrl
                },
                AutoReturn = "approved",
                NotificationUrl = $"{_options.WebhookUrl}?companySlug={encodedCompanySlug}",
                ExternalReference = checkout.Id.ToString(),
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