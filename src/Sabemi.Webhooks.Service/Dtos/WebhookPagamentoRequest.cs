using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sabemi.Webhooks.Service.Dtos;

/// <summary>
/// Payload enviado pelo banco parceiro no webhook de pagamento.
/// </summary>
public class WebhookPagamentoRequest
{
    [JsonPropertyName("id_transacao")]
    [Required(ErrorMessage = "id_transacao é obrigatório.")]
    public string IdTransacao { get; set; } = default!;

    [JsonPropertyName("id_contrato")]
    [Required(ErrorMessage = "id_contrato é obrigatório.")]
    public string IdContrato { get; set; } = default!;

    [JsonPropertyName("valor")]
    [Range(0.01, double.MaxValue, ErrorMessage = "valor deve ser maior que zero.")]
    public decimal Valor { get; set; }

    [JsonPropertyName("data_pagamento")]
    public DateTime DataPagamento { get; set; }

    private string _status = default!;

    [JsonPropertyName("status")]
    [Required(ErrorMessage = "status é obrigatório.")]
    [AllowedValues("aprovado", "recusado", "estornado", "pendente",
        ErrorMessage = "status deve ser um dos seguintes valores: aprovado, recusado, estornado, pendente.")]
    public string Status
    {
        get => _status;
        // normaliza a caixa (ex.: "ApRoVaDo" -> "aprovado") antes da validação de AllowedValues rodar.
        set => _status = value?.Trim().ToLowerInvariant() ?? value!;
    }
}
