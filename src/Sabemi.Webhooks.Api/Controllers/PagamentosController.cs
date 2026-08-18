using Microsoft.AspNetCore.Mvc;
using Sabemi.Webhooks.Service.Dtos;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Api.Controllers;

[ApiController]
[Route("pagamentos")]
public class PagamentosController(IEventoBrutoLogRepository eventoRepository) : ControllerBase
{
    /// <summary>
    /// Lista os eventos de pagamento recebidos, para exibição no dashboard.
    /// Suporta filtro por status (sucesso/erro/pendente) e por id do contrato.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagamentoListResultDto>> Listar(
        [FromQuery] string? status,
        [FromQuery] string? idContrato,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is < 1 or > 200 ? 20 : tamanhoPagina;

        var (itens, total) = await eventoRepository.ListarAsync(status, idContrato, pagina, tamanhoPagina, ct);

        var dto = new PagamentoListResultDto
        {
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            Itens = itens.Select(e => new PagamentoListItemDto
            {
                Id = e.Id,
                IdTransacao = e.IdTransacao,
                IdContrato = e.IdContrato,
                Valor = e.Valor,
                DataPagamento = e.DataPagamento,
                StatusRecebido = e.StatusRecebido,
                StatusProcessamento = e.StatusProcessamento.ToString(),
                MensagemErro = e.MensagemErro,
                RecebidoEm = e.RecebidoEm,
                ProcessadoEm = e.ProcessadoEm
            }).ToList()
        };

        return Ok(dto);
    }
}
