namespace Sabemi.Webhooks.Service.Interfaces;

/// <summary>
/// Fila em memória usada para desacoplar o recebimento do webhook (resposta rápida)
/// do processamento pesado da regra de negócio, que ocorre em background.
/// </summary>
public interface IBackgroundTaskQueue
{
    ValueTask EnfileirarAsync(Guid eventoId, CancellationToken ct);

    IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken ct);
}
