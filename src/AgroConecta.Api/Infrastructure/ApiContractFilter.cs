using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace AgroConecta.Api.Infrastructure;
public class ApiContractFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }];
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente ou inválido" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Perfil sem permissão" });
        }
        operation.Responses.TryAdd("429", new OpenApiResponse { Description = "Rate limit; respeite Retry-After" });
        if (context.ApiDescription.HttpMethod == "POST" && !context.ApiDescription.RelativePath!.StartsWith("api/auth"))
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter { Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true,
                Description = "Chave única por operação, de 1 a 128 caracteres. Repita a chave e o mesmo JSON para recuperar a resposta original.", Schema = new OpenApiSchema { Type = "string", MaxLength = 128 } });
            operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Chave reutilizada com outro conteúdo ou conflito de dados" });
        }
    }
}
