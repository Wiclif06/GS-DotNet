using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Domain.Enums;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class CropAreasEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crop-areas").WithTags("CropAreas").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapPut("/{id:int}", Update).RequireAuthorization("Write").Produces(204).ProducesProblem(404);
        group.MapDelete("/{id:int}", Delete).RequireAuthorization("Write").Produces(204).ProducesProblem(409);
        group.MapGet("/", GetAll).WithName("CropAreasEndpoints_GetAll").Produces<CropArea[]>();
        group.MapGet("/{id:int}", GetById).WithName("CropAreasEndpoints_GetById").Produces<CropArea>();
        group.MapPost("/", Create).WithName("CropAreasEndpoints_Create").Produces<CropArea>(201).ProducesValidationProblem().RequireAuthorization("Write");
    }


    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.CropAreas.OrderBy(c => c.Id).ToListAsync());

    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var cropArea = await db.CropAreas
            .Include(c => c.Sensors)
            .Include(c => c.SatelliteData)
            .Include(c => c.ClimateAlerts)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cropArea is null ? Results.NotFound() : Results.Ok(cropArea);
    }

    public static async Task<IResult> Create(CropAreaRequest request, AgroConectaDbContext db)
    {
        if (!await db.Farms.AnyAsync(f => f.Id == request.FarmId))
            return Results.BadRequest("Fazenda não encontrada.");

        var cropArea = new CropArea
        {
            Name = request.Name,
            CropType = request.CropType,
            AreaSize = request.AreaSize,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            FarmId = request.FarmId,
            Status = CropStatus.NORMAL
        };

        db.CropAreas.Add(cropArea);
        await db.SaveChangesAsync();
        return Results.Created($"/api/crop-areas/{cropArea.Id}", cropArea);
    }

    public static async Task<IResult> Update(int id, CropAreaRequest request, AgroConectaDbContext db)
    {
        var item = await db.CropAreas.FindAsync(id);
        if (item is null) return Results.NotFound();
        if (!await db.Farms.AnyAsync(f => f.Id == request.FarmId)) return Results.BadRequest("Fazenda não encontrada.");
        item.Name = request.Name; item.CropType = request.CropType; item.AreaSize = request.AreaSize;
        item.Latitude = request.Latitude; item.Longitude = request.Longitude; item.FarmId = request.FarmId;
        await db.SaveChangesAsync(); return Results.NoContent();
    }
    public static async Task<IResult> Delete(int id, AgroConectaDbContext db)
    {
        var item = await db.CropAreas.FindAsync(id);
        if (item is null) return Results.NotFound();
        if (await db.Sensors.AnyAsync(x => x.CropAreaId == id) || await db.SatelliteData.AnyAsync(x => x.CropAreaId == id)
            || await db.ClimateAlerts.AnyAsync(x => x.CropAreaId == id))
            return Results.Conflict(new { error = "Talhão possui histórico; exclusão bloqueada para preservar os registros." });
        db.CropAreas.Remove(item); await db.SaveChangesAsync(); return Results.NoContent();
    }
}
