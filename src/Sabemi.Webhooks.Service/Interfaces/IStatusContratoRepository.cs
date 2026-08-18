using Sabemi.Webhooks.Model.Entities;

namespace Sabemi.Webhooks.Service.Interfaces;

public interface IStatusContratoRepository
{
    Task<StatusContrato?> ObterPorIdContratoAsync(string idContrato, CancellationToken ct);

    Task SalvarAsync(StatusContrato statusContrato, CancellationToken ct);
}
