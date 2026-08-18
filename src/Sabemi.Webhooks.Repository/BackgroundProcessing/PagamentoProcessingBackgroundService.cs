using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Repository.BackgroundProcessing;

/// <summary>
/// Worker que consome a fila de eventos pendentes e dispara o processamento
/// da regra de negócio, um por vez, fora do ciclo de resposta HTTP do webhook.
/// </summary>
public class PagamentoProcessingBackgroundService(
    IBackgroundTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PagamentoProcessingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var eventoId in queue.ConsumirAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredService<IPagamentoProcessingService>();
                await processingService.ProcessarAsync(eventoId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado ao processar evento {EventoId} em background.", eventoId);
            }
        }
    }
}
