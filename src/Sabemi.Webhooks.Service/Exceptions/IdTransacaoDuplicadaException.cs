namespace Sabemi.Webhooks.Service.Exceptions;

/// <summary>
/// Lançada pelo repositório quando a constraint única de id_transacao é violada
/// (corrida entre duas requisições concorrentes com o mesmo id_transacao).
/// </summary>
public class IdTransacaoDuplicadaException(string idTransacao)
    : Exception($"id_transacao '{idTransacao}' já existe.")
{
    public string IdTransacao { get; } = idTransacao;
}
