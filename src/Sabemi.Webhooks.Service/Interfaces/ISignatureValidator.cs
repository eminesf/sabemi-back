namespace Sabemi.Webhooks.Service.Interfaces;

/// <summary>
/// Valida a assinatura HMAC enviada pelo banco parceiro no header da requisição,
/// garantindo autenticidade e integridade do corpo (raw body) recebido.
/// </summary>
public interface ISignatureValidator
{
    bool IsValid(string rawBody, string? signatureHeaderValue);
}
