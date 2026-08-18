namespace Sabemi.Webhooks.Service.Interfaces;

/// <summary>
/// Valida a API Key enviada pelo banco parceiro no header da requisição.
/// </summary>
public interface IApiKeyValidator
{
    bool IsValid(string? apiKeyHeaderValue);
}
