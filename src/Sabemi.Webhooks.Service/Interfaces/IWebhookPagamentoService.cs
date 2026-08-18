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
    /// Registra uma tentativa que falhou na validação de API Key/formato/dados,
    /// para que apareça no painel com alerta de erro. Quando o JSON foi parseado
    /// com sucesso (falha em validação de campo, por exemplo), passe o payload
    /// pra preservar os dados recebidos no log — só o id_transacao continua
    /// sintético, pra não travar a idempotência de um reenvio corrigido.
    /// </summary>
    Task RegistrarFalhaValidacaoAsync(string rawBody, string motivo, CancellationToken ct, WebhookPagamentoRequest? payload = null);
}
