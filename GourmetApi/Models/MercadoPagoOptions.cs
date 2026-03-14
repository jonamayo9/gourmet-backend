namespace GourmetApi.Models;

public class MercadoPagoOptions
{
    public string AccessToken { get; set; } = "";
    public string SuccessUrl { get; set; } = "";
    public string FailureUrl { get; set; } = "";
    public string PendingUrl { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public string FrontendBaseUrl { get; set; } = string.Empty;
}