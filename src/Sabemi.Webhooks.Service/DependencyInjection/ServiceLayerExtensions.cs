using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Service.Services;

namespace Sabemi.Webhooks.Service.DependencyInjection;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Webhook:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configuração 'Webhook:ApiKey' não definida.");

        services.AddSingleton<IApiKeyValidator>(new ApiKeyValidator(apiKey));
        services.AddScoped<IWebhookPagamentoService, WebhookPagamentoService>();
        services.AddScoped<IPagamentoProcessingService, PagamentoProcessingService>();

        return services;
    }
}
