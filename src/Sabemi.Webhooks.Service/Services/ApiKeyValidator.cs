using System.Security.Cryptography;
using System.Text;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Service.Services;

/// <summary>
/// Compara a API Key recebida no header com a configurada (Webhook:ApiKey).
/// Comparação em tempo constante para evitar timing attack, mas sem cálculo
/// de hash algum — o banco parceiro só precisa enviar a chave combinada
/// direto no header, sem assinar nada.
/// </summary>
public class ApiKeyValidator : IApiKeyValidator
{
    private readonly byte[] _apiKeyBytes;

    public ApiKeyValidator(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key do webhook (Webhook:ApiKey) não configurada.", nameof(apiKey));

        _apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public bool IsValid(string? apiKeyHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(apiKeyHeaderValue))
            return false;

        var recebidaBytes = Encoding.UTF8.GetBytes(apiKeyHeaderValue.Trim());

        if (recebidaBytes.Length != _apiKeyBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(_apiKeyBytes, recebidaBytes);
    }
}
