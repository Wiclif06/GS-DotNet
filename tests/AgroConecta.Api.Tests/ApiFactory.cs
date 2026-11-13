using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroConecta.Api.Tests;
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"agroconecta-tests-{Guid.NewGuid():N}.db");
    public int Limit { get; set; } = 1000;
    public string? SharedDatabasePath { get; set; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite", ["ConnectionStrings:DefaultConnection"] = $"Data Source={SharedDatabasePath ?? path}",
            ["Jwt:Key"] = "Test-only-signing-key-with-more-than-32-bytes!", ["Jwt:Issuer"] = "AgroConecta",
            ["Jwt:Audience"] = "AgroConecta.Mobile", ["Seed:AdminEmail"] = "admin@test.local",
            ["Seed:AdminPassword"] = "Integration-test-password!", ["RateLimiting:PermitLimit"] = Limit.ToString()
        }));
    }
    public async Task AddReaderAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroConectaDbContext>();
        var reader = new User { Name = "Leitor", Email = "reader@test.local", Role = "READER" };
        reader.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(reader, "Integration-test-password!");
        db.Users.Add(reader); await db.SaveChangesAsync();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(path)) File.Delete(path);
    }
}
