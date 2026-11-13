using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class RecommendationsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recommendations").WithTags("Recommendations").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/{id:int}", GetById).WithName("Recommendations_GetById").Produces<Recommendation>().ProducesProblem(404);
        group.MapGet("/", GetAll).WithName("RecommendationsEndpoints_GetAll").Produces<Recommendation[]>();
        group.MapPost("/", Create).WithName("RecommendationsEndpoints_Create").Produces<Recommendation>(201).ProducesValidationProblem().RequireAuthorization("Write");
    }


    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var item = await db.Recommendations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.Recommendations.OrderByDescending(r => r.CreatedAt).ToListAsync());

    public static async Task<IResult> Create(RecommendationRequest request, AgroConectaDbContext db)
    {
        if (!await db.ClimateAlerts.AnyAsync(a => a.Id == request.ClimateAlertId))
            return Results.BadRequest("Alerta não encontrado.");
        if (!Enum.TryParse<AgroConecta.Api.Domain.Enums.AlertSeverity>(request.Priority, true, out var priority) || !Enum.IsDefined(priority))
            return Results.BadRequest("Prioridade inválida.");

        var recommendation = new Recommendation
        {
            ClimateAlertId = request.ClimateAlertId,
            Message = request.Message,
            Priority = priority.ToString()
        };

        db.Recommendations.Add(recommendation);
        await db.SaveChangesAsync();
        return Results.Created($"/api/recommendations/{recommendation.Id}", recommendation);
    }
}
