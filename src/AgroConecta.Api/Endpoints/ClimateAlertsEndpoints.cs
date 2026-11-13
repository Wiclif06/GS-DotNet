using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Domain.Enums;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class ClimateAlertsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/climate-alerts").WithTags("ClimateAlerts").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/{id:int}", GetById).WithName("ClimateAlerts_GetById").Produces<ClimateAlert>().ProducesProblem(404);
        group.MapGet("/", GetAll).WithName("ClimateAlertsEndpoints_GetAll").Produces<ClimateAlert[]>();
        group.MapPost("/", Create).WithName("ClimateAlertsEndpoints_Create").Produces<ClimateAlert>(201).ProducesValidationProblem().RequireAuthorization("Write");
        group.MapPut("/{id:int}/resolve", Resolve).WithName("ClimateAlertsEndpoints_Resolve").Produces(204).ProducesProblem(404).RequireAuthorization("Write");
    }


    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var item = await db.ClimateAlerts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.ClimateAlerts.Include(a => a.Recommendations).OrderByDescending(a => a.CreatedAt).ToListAsync());

    public static async Task<IResult> Create(ClimateAlertRequest request, AgroConectaDbContext db)
    {
        if (!Enum.TryParse<AlertSeverity>(request.Severity, true, out var severity) || !Enum.IsDefined(severity))
            return Results.BadRequest("Severidade inválida. Use LOW, MEDIUM, HIGH ou CRITICAL.");

        if (!await db.CropAreas.AnyAsync(c => c.Id == request.CropAreaId))
            return Results.BadRequest("Talhão não encontrado.");

        var alert = new ClimateAlert
        {
            CropAreaId = request.CropAreaId,
            AlertType = request.AlertType,
            Title = request.Title,
            Description = request.Description,
            Severity = severity
        };

        db.ClimateAlerts.Add(alert);
        await db.SaveChangesAsync();
        return Results.Created($"/api/climate-alerts/{alert.Id}", alert);
    }

    public static async Task<IResult> Resolve(int id, AgroConectaDbContext db)
    {
        var alert = await db.ClimateAlerts.FindAsync(id);
        if (alert is null) return Results.NotFound();
        if (alert.Status == AlertStatus.RESOLVED) return Results.NoContent();

        alert.Status = AlertStatus.RESOLVED;
        alert.ResolvedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
