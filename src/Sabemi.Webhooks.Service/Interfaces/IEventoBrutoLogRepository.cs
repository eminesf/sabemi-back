using Sabemi.Webhooks.Model.Entities;

namespace Sabemi.Webhooks.Service.Interfaces;

public interface IEventoBrutoLogRepository
{
    Task<EventoBrutoLog?> ObterPorIdTransacaoAsync(string idTransacao, CancellationToken ct);

    Task<EventoBrutoLog?> ObterPorIdAsync(Guid id, CancellationToken ct);

    Task AdicionarAsync(EventoBrutoLog evento, CancellationToken ct);

    Task AtualizarAsync(EventoBrutoLog evento, CancellationToken ct);

    Task<(IReadOnlyList<EventoBrutoLog> Itens, int Total)> ListarAsync(
        string? status,
        string? idContrato,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct);
}
