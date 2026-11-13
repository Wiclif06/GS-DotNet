using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class SensorsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/{id:int}", GetById).WithName("Sensors_GetById").Produces<Sensor>().ProducesProblem(404);
        group.MapGet("/", GetAll).WithName("SensorsEndpoints_GetAll").Produces<Sensor[]>();
        group.MapPost("/", Create).WithName("SensorsEndpoints_Create").Produces<Sensor>(201).ProducesValidationProblem().RequireAuthorization("Write");
    }


    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var item = await db.Sensors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.Sensors.Include(s => s.Readings).ToListAsync());

    public static async Task<IResult> Create(SensorRequest request, AgroConectaDbContext db)
    {
        if (!await db.CropAreas.AnyAsync(c => c.Id == request.CropAreaId))
            return Results.BadRequest("Talhão não encontrado.");

        var sensor = new Sensor { Name = request.Name, SensorType = request.SensorType, CropAreaId = request.CropAreaId };
        db.Sensors.Add(sensor);
        await db.SaveChangesAsync();
        return Results.Created($"/api/sensors/{sensor.Id}", sensor);
    }
}
