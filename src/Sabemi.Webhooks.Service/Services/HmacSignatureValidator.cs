using System.Security.Cryptography;
using System.Text;
using Sabemi.Webhooks.Service.Interfaces;

namespace Sabemi.Webhooks.Service.Services;

/// <summary>
/// Valida a assinatura enviada pelo banco parceiro recalculando o HMAC-SHA256
/// do corpo bruto da requisição com um segredo compartilhado, e comparando
/// os hashes em tempo constante (evita timing attack).
/// </summary>
public class HmacSignatureValidator : ISignatureValidator
{
    private readonly byte[] _secretBytes;

    public HmacSignatureValidator(string sharedSecret)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret))
            throw new ArgumentException("Segredo do webhook (Webhook:SharedSecret) não configurado.", nameof(sharedSecret));

        _secretBytes = Encoding.UTF8.GetBytes(sharedSecret);
    }

    public bool IsValid(string rawBody, string? signatureHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(signatureHeaderValue))
            return false;

        var recebida = signatureHeaderValue.Trim();
        if (recebida.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            recebida = recebida["sha256=".Length..];

        using var hmac = new HMACSHA256(_secretBytes);
        var hashCalculado = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var hexCalculado = Convert.ToHexStringLower(hashCalculado);

        var bytesEsperados = Encoding.UTF8.GetBytes(hexCalculado);
        var bytesRecebidos = Encoding.UTF8.GetBytes(recebida.ToLowerInvariant());

        if (bytesEsperados.Length != bytesRecebidos.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecebidos);
    }
}
