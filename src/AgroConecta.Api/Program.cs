using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Endpoints;
using AgroConecta.Api.Infrastructure;
using AgroConecta.Api.Middlewares;
using AgroConecta.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AgroConectaDbContext>(options =>
{
    if (builder.Configuration["Database:Provider"] == "Sqlite")
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=agroconecta.db");
    else options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection."));
});
builder.Services.AddScoped<RiskAnalysisService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)), ClockSkew = TimeSpan.FromSeconds(10)
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Write", p => p.RequireRole("ADMIN", "PRODUCER"));
    o.AddPolicy("Admin", p => p.RequireRole("ADMIN"));
});
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            { PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: 429, title: "Limite de requisições excedido.").ExecuteAsync(context.HttpContext);
    };
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    if (origins.Length > 0) p.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location", "Retry-After", "Idempotency-Replayed");
}));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroConecta CitizenTech API", Version = "v1", Description = "Monitoramento rural comunitário. Escritas POST exigem Idempotency-Key. Perfis ADMIN, PRODUCER e READER." });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.OperationFilter<AgroConecta.Api.Infrastructure.ApiContractFilter>();
    o.SchemaFilter<JsonEnumSchemaFilter>();
});
var app = builder.Build();
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Configure Jwt__Key com ao menos 32 bytes; não armazene o segredo no repositório.");

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors(); app.UseAuthentication(); app.UseRateLimiter(); app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();
AuthEndpoints.Map(app);
UsersEndpoints.Map(app); FarmsEndpoints.Map(app); CropAreasEndpoints.Map(app); SensorsEndpoints.Map(app);
SensorReadingsEndpoints.Map(app); SatelliteDataEndpoints.Map(app); ClimateAlertsEndpoints.Map(app);
RecommendationsEndpoints.Map(app); DashboardEndpoints.Map(app);
app.MapGet("/", () => Results.Ok(new { app = "AgroConecta CitizenTech", swagger = "/swagger", health = "/health" }));
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealth });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealth });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteHealth });
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgroConectaDbContext>();
    if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();
    await SeedData.InsertAsync(db, builder.Configuration, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}
app.Run();
static Task WriteHealth(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report) =>
    context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }) });
public partial class Program { }
