using System.Threading.Channels;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Repository.BackgroundProcessing;

/// <summary>
/// Fila em memória (System.Threading.Channels) usada para desacoplar o endpoint
/// HTTP (que responde rápido) do processamento pesado, que roda em background.
/// Simples e suficiente para o escopo deste teste; em produção poderia ser
/// substituída por um message broker (ex.: RabbitMQ/SQS) sem alterar os
/// consumidores, pois ambos dependem apenas de IBackgroundTaskQueue.
/// </summary>
public class ChannelBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public async ValueTask EnfileirarAsync(Guid eventoId, CancellationToken ct) =>
        await _channel.Writer.WriteAsync(eventoId, ct);

    public IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
