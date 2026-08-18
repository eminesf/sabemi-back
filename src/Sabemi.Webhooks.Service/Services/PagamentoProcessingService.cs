using Microsoft.Extensions.Logging;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Model.Entities;
using Sabemi.Webhooks.Model.Enums;

namespace Sabemi.Webhooks.Service.Services;

/// <summary>
/// Regra de negócio "pesada" executada em background pelo worker, fora do ciclo
/// de resposta HTTP do webhook: valida o pagamento e atualiza o status do contrato.
/// </summary>
public class PagamentoProcessingService : IPagamentoProcessingService
{
    private static readonly TimeSpan SimulacaoProcessamentoPesado = TimeSpan.FromSeconds(2);

    private readonly IEventoBrutoLogRepository _eventoRepository;
    private readonly IStatusContratoRepository _statusContratoRepository;
    private readonly ILogger<PagamentoProcessingService> _logger;

    public PagamentoProcessingService(
        IEventoBrutoLogRepository eventoRepository,
        IStatusContratoRepository statusContratoRepository,
        ILogger<PagamentoProcessingService> logger)
    {
        _eventoRepository = eventoRepository;
        _statusContratoRepository = statusContratoRepository;
        _logger = logger;
    }

    public async Task ProcessarAsync(Guid eventoId, CancellationToken ct)
    {
        var evento = await _eventoRepository.ObterPorIdAsync(eventoId, ct);
        if (evento is null)
        {
            _logger.LogWarning("Evento {EventoId} não encontrado para processamento.", eventoId);
            return;
        }

        evento.StatusProcessamento = StatusProcessamento.Processando;
        await _eventoRepository.AtualizarAsync(evento, ct);

        // simula regra de negócio pesada (ex.: chamada a sistema de apólices/contratos)
        await Task.Delay(SimulacaoProcessamentoPesado, ct);

        try
        {
            if (evento.Valor <= 0)
                throw new InvalidOperationException("Valor de pagamento inválido (deve ser maior que zero).");

            if (string.IsNullOrWhiteSpace(evento.IdContrato))
                throw new InvalidOperationException("id_contrato não informado.");

            var statusContrato = await _statusContratoRepository.ObterPorIdContratoAsync(evento.IdContrato, ct)
                ?? new StatusContrato { Id = Guid.NewGuid(), IdContrato = evento.IdContrato };

            statusContrato.UltimoStatus = evento.StatusRecebido;
            statusContrato.UltimoValor = evento.Valor;
            statusContrato.UltimaDataPagamento = evento.DataPagamento;
            statusContrato.UltimaTransacaoId = evento.IdTransacao;
            statusContrato.AtualizadoEm = DateTime.UtcNow;

            await _statusContratoRepository.SalvarAsync(statusContrato, ct);

            evento.StatusProcessamento = StatusProcessamento.Concluido;
            evento.MensagemErro = null;
        }
        catch (Exception ex)
        {
            evento.StatusProcessamento = StatusProcessamento.ErroProcessamento;
            evento.MensagemErro = ex.Message;
            _logger.LogError(ex, "Falha ao processar evento {EventoId} (id_transacao {IdTransacao}).", evento.Id, evento.IdTransacao);
        }

        evento.ProcessadoEm = DateTime.UtcNow;
        await _eventoRepository.AtualizarAsync(evento, ct);
    }
}
