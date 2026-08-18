using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sabemi.Webhooks.Service.Dtos;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController(
    ISignatureValidator signatureValidator,
    IWebhookPagamentoService webhookPagamentoService,
    ILogger<WebhooksController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Recebe notificações de pagamento do banco parceiro. Responde rapidamente
    /// (idempotência resolvida de forma síncrona) e delega o processamento pesado
    /// da regra de negócio para um worker em background.
    /// </summary>
    [HttpPost("pagamento")]
    public async Task<IActionResult> ReceberPagamento(CancellationToken ct)
    {
        var rawBody = await LerRawBodyAsync(ct);

        Request.Headers.TryGetValue("X-Signature", out var signatureHeader);

        if (!signatureValidator.IsValid(rawBody, signatureHeader))
        {
            logger.LogWarning("Webhook rejeitado: assinatura ausente ou inválida.");
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, "Assinatura ausente ou inválida (header X-Signature).", ct);
            return Unauthorized(new { erro = "Assinatura inválida." });
        }

        WebhookPagamentoRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPagamentoRequest>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, "Corpo da requisição não é um JSON válido.", ct);
            return BadRequest(new { erro = "JSON inválido." });
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.IdTransacao))
        {
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, "Campo id_transacao é obrigatório.", ct);
            return BadRequest(new { erro = "id_transacao é obrigatório." });
        }

        var resultado = await webhookPagamentoService.ReceberAsync(payload, rawBody, ct);

        if (resultado.JaProcessado)
        {
            return Ok(new { mensagem = "Evento já recebido anteriormente (idempotência).", idTransacao = payload.IdTransacao });
        }

        return Accepted(new { mensagem = "Evento recebido, processamento em andamento.", idTransacao = payload.IdTransacao });
    }

    private async Task<string> LerRawBodyAsync(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;
        return rawBody;
    }
}
