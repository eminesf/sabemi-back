using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Service.Services;

namespace Sabemi.Webhooks.Service.DependencyInjection;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var sharedSecret = configuration["Webhook:SharedSecret"];
        if (string.IsNullOrWhiteSpace(sharedSecret))
            throw new InvalidOperationException("Configuração 'Webhook:SharedSecret' não definida.");

        services.AddSingleton<ISignatureValidator>(new HmacSignatureValidator(sharedSecret));
        services.AddScoped<IWebhookPagamentoService, WebhookPagamentoService>();
        services.AddScoped<IPagamentoProcessingService, PagamentoProcessingService>();

        return services;
    }
}
