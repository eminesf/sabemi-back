using Microsoft.Extensions.Logging;
using Sabemi.Webhooks.Service.Dtos;
using Sabemi.Webhooks.Service.Exceptions;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Model.Entities;
using Sabemi.Webhooks.Model.Enums;

namespace Sabemi.Webhooks.Service.Services;

public class WebhookPagamentoService : IWebhookPagamentoService
{
    private readonly IEventoBrutoLogRepository _eventoRepository;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ILogger<WebhookPagamentoService> _logger;

    public WebhookPagamentoService(
        IEventoBrutoLogRepository eventoRepository,
        IBackgroundTaskQueue queue,
        ILogger<WebhookPagamentoService> logger)
    {
        _eventoRepository = eventoRepository;
        _queue = queue;
        _logger = logger;
    }

    public async Task<ReceberWebhookResultado> ReceberAsync(WebhookPagamentoRequest payload, string rawBody, CancellationToken ct)
    {
        var existente = await _eventoRepository.ObterPorIdTransacaoAsync(payload.IdTransacao, ct);
        if (existente is not null)
        {
            _logger.LogInformation(
                "id_transacao {IdTransacao} já recebido anteriormente (idempotência) — ignorando reprocessamento.",
                payload.IdTransacao);
            return new ReceberWebhookResultado(JaProcessado: true, existente.Id);
        }

        var evento = new EventoBrutoLog
        {
            Id = Guid.NewGuid(),
            IdTransacao = payload.IdTransacao,
            IdContrato = payload.IdContrato,
            Valor = payload.Valor,
            DataPagamento = payload.DataPagamento,
            StatusRecebido = payload.Status,
            PayloadBruto = rawBody,
            RecebidoEm = DateTime.UtcNow,
            StatusProcessamento = StatusProcessamento.Pendente
        };

        try
        {
            await _eventoRepository.AdicionarAsync(evento, ct);
        }
        catch (IdTransacaoDuplicadaException)
        {
            // condição de corrida: duas requisições concorrentes com o mesmo id_transacao.
            // a constraint única do banco garante que só uma foi persistida.
            var concorrente = await _eventoRepository.ObterPorIdTransacaoAsync(payload.IdTransacao, ct);
            _logger.LogWarning(
                "Conflito de idempotência (corrida) detectado para id_transacao {IdTransacao}.",
                payload.IdTransacao);
            return new ReceberWebhookResultado(JaProcessado: true, concorrente?.Id ?? evento.Id);
        }

        await _queue.EnfileirarAsync(evento.Id, ct);

        return new ReceberWebhookResultado(JaProcessado: false, evento.Id);
    }

    public async Task RegistrarFalhaValidacaoAsync(string rawBody, string motivo, CancellationToken ct)
    {
        var evento = new EventoBrutoLog
        {
            Id = Guid.NewGuid(),
            IdTransacao = $"INVALIDO-{Guid.NewGuid():N}",
            PayloadBruto = rawBody,
            RecebidoEm = DateTime.UtcNow,
            StatusProcessamento = StatusProcessamento.ErroValidacao,
            MensagemErro = motivo
        };

        await _eventoRepository.AdicionarAsync(evento, ct);
        _logger.LogWarning("Evento rejeitado na validação: {Motivo}", motivo);
    }
}
