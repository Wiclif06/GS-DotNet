using AgroConecta.Api.Data;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace AgroConecta.Api.Tests;

public class DatabaseHealthTests
{
    [Fact]
    public async Task Unavailable_database_is_reported_as_unhealthy()
    {
        // Read-only mode cannot silently create the missing database.
        var path = Path.Combine(Path.GetTempPath(), $"missing-agroconecta-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddDbContext<AgroConectaDbContext>(options => options.UseSqlite($"Data Source={path};Mode=ReadOnly"));
        using var provider = services.BuildServiceProvider();
        var check = new DatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Null(result.Exception);
        Assert.False(File.Exists(path));
    }
}
