using Sabemi.Webhooks.Model.Enums;

namespace Sabemi.Webhooks.Model.Entities;

/// <summary>
/// Registro bruto de cada notificação recebida do banco parceiro (log de eventos),
/// incluindo tentativas que falharam na validação de assinatura.
/// </summary>
public class EventoBrutoLog
{
    public Guid Id { get; set; }

    public string IdTransacao { get; set; } = default!;

    public string? IdContrato { get; set; }

    public decimal Valor { get; set; }

    public DateTime? DataPagamento { get; set; }

    public string? StatusRecebido { get; set; }

    public string PayloadBruto { get; set; } = default!;

    public DateTime RecebidoEm { get; set; }

    public StatusProcessamento StatusProcessamento { get; set; } = StatusProcessamento.Pendente;

    public string? MensagemErro { get; set; }

    public DateTime? ProcessadoEm { get; set; }
}
