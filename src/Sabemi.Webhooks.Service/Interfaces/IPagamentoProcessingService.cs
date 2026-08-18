namespace Sabemi.Webhooks.Service.Interfaces;

/// <summary>
/// Executa a regra de negócio "pesada" de processamento do evento de pagamento
/// (chamado pelo worker em background, fora do ciclo de resposta HTTP).
/// </summary>
public interface IPagamentoProcessingService
{
    Task ProcessarAsync(Guid eventoId, CancellationToken ct);
}
