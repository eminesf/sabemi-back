namespace Sabemi.Webhooks.Model.Entities;

public class StatusContrato
{
    public Guid Id { get; set; }

    public string IdContrato { get; set; } = default!;

    public string? UltimoStatus { get; set; }

    public decimal? UltimoValor { get; set; }

    public DateTime? UltimaDataPagamento { get; set; }

    public string? UltimaTransacaoId { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
