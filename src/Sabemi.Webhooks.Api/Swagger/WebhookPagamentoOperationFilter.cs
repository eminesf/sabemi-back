using Microsoft.OpenApi.Models;
using Sabemi.Webhooks.Api.Controllers;
using Sabemi.Webhooks.Service.Dtos;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sabemi.Webhooks.Api.Swagger;

/// <summary>
/// O corpo e o header de API Key de <see cref="WebhooksController.ReceberPagamento"/> não
/// são bindados via [FromBody]/[FromHeader] (o controller lê o raw body manualmente, para
/// poder logar o payload bruto em caso de falha de validação). Sem essas anotações o
/// Swashbuckle não gera os campos correspondentes no Swagger UI. Este filtro documenta o
/// schema do body (WebhookPagamentoRequest) e o header X-Api-Key só para a UI/OpenAPI, sem
/// alterar o binding em si.
/// </summary>
public class WebhookPagamentoOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(WebhooksController)
            || context.MethodInfo.Name != nameof(WebhooksController.ReceberPagamento))
            return;

        var schema = context.SchemaGenerator.GenerateSchema(typeof(WebhookPagamentoRequest), context.SchemaRepository);

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = schema }
            }
        };

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Chave combinada com o banco parceiro (Webhook:ApiKey). Envie o valor exatamente como configurado, sem hash nem cálculo nenhum."
        });
    }
}
