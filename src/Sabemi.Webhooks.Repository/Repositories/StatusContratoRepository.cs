using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Model.Entities;
using Sabemi.Webhooks.Repository.Persistence;

namespace Sabemi.Webhooks.Repository.Repositories;

public class StatusContratoRepository(SabemiDbContext dbContext) : IStatusContratoRepository
{
    public Task<StatusContrato?> ObterPorIdContratoAsync(string idContrato, CancellationToken ct) =>
        dbContext.StatusContratos.FirstOrDefaultAsync(s => s.IdContrato == idContrato, ct);

    public async Task SalvarAsync(StatusContrato statusContrato, CancellationToken ct)
    {
        var existente = await dbContext.StatusContratos
            .FirstOrDefaultAsync(s => s.IdContrato == statusContrato.IdContrato, ct);

        if (existente is null)
        {
            dbContext.StatusContratos.Add(statusContrato);
        }
        else
        {
            existente.UltimoStatus = statusContrato.UltimoStatus;
            existente.UltimoValor = statusContrato.UltimoValor;
            existente.UltimaDataPagamento = statusContrato.UltimaDataPagamento;
            existente.UltimaTransacaoId = statusContrato.UltimaTransacaoId;
            existente.AtualizadoEm = statusContrato.AtualizadoEm;
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
