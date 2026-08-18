namespace Sabemi.Webhooks.Service.Dtos;

/// <summary>
/// Item exibido no dashboard (listagem de eventos de pagamento).
/// </summary>
public class PagamentoListItemDto
{
    public Guid Id { get; set; }
    public string IdTransacao { get; set; } = default!;
    public string? IdContrato { get; set; }
    public decimal Valor { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? StatusRecebido { get; set; }
    public string StatusProcessamento { get; set; } = default!;
    public string? MensagemErro { get; set; }
    public DateTime RecebidoEm { get; set; }
    public DateTime? ProcessadoEm { get; set; }
}

public class PagamentoListResultDto
{
    public IReadOnlyList<PagamentoListItemDto> Itens { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
