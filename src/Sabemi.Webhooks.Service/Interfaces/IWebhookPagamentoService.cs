using Sabemi.Webhooks.Service.Dtos;

namespace Sabemi.Webhooks.Service.Interfaces;

public interface IWebhookPagamentoService
{
    /// <summary>
    /// Recebe um payload já validado (assinatura ok), garante idempotência por
    /// id_transacao, persiste o log bruto e enfileira o processamento em background.
    /// </summary>
    Task<ReceberWebhookResultado> ReceberAsync(WebhookPagamentoRequest payload, string rawBody, CancellationToken ct);

    /// <summary>
    /// Registra uma tentativa que falhou na validação de assinatura/formato,
    /// para que apareça no painel com alerta de erro.
    /// </summary>
    Task RegistrarFalhaValidacaoAsync(string rawBody, string motivo, CancellationToken ct);
}
