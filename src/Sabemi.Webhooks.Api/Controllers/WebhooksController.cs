using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sabemi.Webhooks.Service.Dtos;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController(
    IApiKeyValidator apiKeyValidator,
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

        Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader);

        if (!apiKeyValidator.IsValid(apiKeyHeader))
        {
            var motivo = string.IsNullOrWhiteSpace(apiKeyHeader)
                ? "Header X-Api-Key ausente."
                : "API Key inválida.";

            logger.LogWarning("Webhook rejeitado: {Motivo}", motivo);
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, motivo, ct);
            return Unauthorized(new { erro = motivo });
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

        if (payload is null)
        {
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, "Corpo da requisição vazio.", ct);
            return BadRequest(new { erro = "Corpo da requisição vazio." });
        }

        var errosValidacao = new List<ValidationResult>();
        if (!Validator.TryValidateObject(payload, new ValidationContext(payload), errosValidacao, validateAllProperties: true))
        {
            var detalhes = errosValidacao
                .Select(e => new { campo = string.Join(", ", e.MemberNames), mensagem = e.ErrorMessage })
                .ToList();

            var motivo = string.Join(" | ", detalhes.Select(d => $"{d.campo}: {d.mensagem}"));
            logger.LogWarning("Webhook rejeitado: dados inválidos ({Motivo})", motivo);
            await webhookPagamentoService.RegistrarFalhaValidacaoAsync(rawBody, motivo, ct, payload);
            return BadRequest(new { erro = "Dados inválidos.", detalhes });
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
