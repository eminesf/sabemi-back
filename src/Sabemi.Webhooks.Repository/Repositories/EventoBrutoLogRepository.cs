using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Service.Exceptions;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Model.Entities;
using Sabemi.Webhooks.Model.Enums;
using Sabemi.Webhooks.Repository.Persistence;

namespace Sabemi.Webhooks.Repository.Repositories;

public class EventoBrutoLogRepository(SabemiDbContext dbContext) : IEventoBrutoLogRepository
{
    public Task<EventoBrutoLog?> ObterPorIdTransacaoAsync(string idTransacao, CancellationToken ct) =>
        dbContext.EventosBrutos.FirstOrDefaultAsync(e => e.IdTransacao == idTransacao, ct);

    public Task<EventoBrutoLog?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        dbContext.EventosBrutos.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AdicionarAsync(EventoBrutoLog evento, CancellationToken ct)
    {
        dbContext.EventosBrutos.Add(evento);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new IdTransacaoDuplicadaException(evento.IdTransacao);
        }
    }

    public async Task AtualizarAsync(EventoBrutoLog evento, CancellationToken ct)
    {
        dbContext.EventosBrutos.Update(evento);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken ct)
    {
        var evento = await dbContext.EventosBrutos.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (evento is null)
            return false;

        dbContext.EventosBrutos.Remove(evento);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(IReadOnlyList<EventoBrutoLog> Itens, int Total)> ListarAsync(
        string? status,
        string? idContrato,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct)
    {
        var query = dbContext.EventosBrutos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(idContrato))
            query = query.Where(e => EF.Functions.ILike(e.IdContrato!, $"%{idContrato}%"));

        query = status?.Trim().ToLowerInvariant() switch
        {
            "sucesso" => query.Where(e => e.StatusProcessamento == StatusProcessamento.Concluido),
            "erro" => query.Where(e =>
                e.StatusProcessamento == StatusProcessamento.ErroValidacao ||
                e.StatusProcessamento == StatusProcessamento.ErroProcessamento),
            "pendente" => query.Where(e =>
                e.StatusProcessamento == StatusProcessamento.Pendente ||
                e.StatusProcessamento == StatusProcessamento.Processando),
            _ => query
        };

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderByDescending(e => e.RecebidoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

        return (itens, total);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}
