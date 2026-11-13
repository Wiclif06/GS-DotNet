using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class SensorReadingsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensor-readings").WithTags("SensorReadings").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/{id:int}", GetById).WithName("SensorReadings_GetById").Produces<SensorReading>().ProducesProblem(404);
        group.MapGet("/", GetAll).WithName("SensorReadingsEndpoints_GetAll").Produces<SensorReading[]>();
        group.MapPost("/", Create).WithName("SensorReadingsEndpoints_Create").Produces<SensorReading>(201).ProducesValidationProblem().RequireAuthorization("Write");
    }


    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var item = await db.SensorReadings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.SensorReadings.OrderByDescending(r => r.ReadingDate).ToListAsync());

    public static async Task<IResult> Create(SensorReadingRequest request, AgroConectaDbContext db)
    {
        if (!await db.Sensors.AnyAsync(s => s.Id == request.SensorId))
            return Results.BadRequest("Sensor não encontrado.");

        var reading = new SensorReading
        {
            SensorId = request.SensorId,
            Temperature = request.Temperature,
            AirHumidity = request.AirHumidity,
            SoilHumidity = request.SoilHumidity,
            ManualAlert = request.ManualAlert
        };

        db.SensorReadings.Add(reading);
        await db.SaveChangesAsync();
        return Results.Created($"/api/sensor-readings/{reading.Id}", reading);
    }
}
