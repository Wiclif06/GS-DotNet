using AgroConecta.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace AgroConecta.Api.Infrastructure;
public class DatabaseHealthCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgroConectaDbContext>();
            return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Banco indisponível.");
        }
        catch { return HealthCheckResult.Unhealthy("Banco indisponível."); }
    }
}
