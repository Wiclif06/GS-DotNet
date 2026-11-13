using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgroConecta.Api.Middlewares;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (BadHttpRequestException ex)
        {
            await Results.Problem(statusCode: ex.StatusCode, title: "Requisição inválida.").ExecuteAsync(context);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        {
            await Results.Problem(statusCode: 409, title: "Conflito de dados ou relacionamento.").ExecuteAsync(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            logger.LogError(ex, "Erro na requisição {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            await Results.Problem(statusCode: 500, title: "Erro interno na API AgroConecta.",
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        }
    }
}
