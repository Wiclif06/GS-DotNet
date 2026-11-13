using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Services;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class SatelliteDataEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/satellite-data").WithTags("SatelliteData").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/{id:int}", GetById).WithName("SatelliteData_GetById").Produces<SatelliteData>().ProducesProblem(404);
        group.MapGet("/", GetAll).WithName("SatelliteDataEndpoints_GetAll").Produces<SatelliteData[]>();
        group.MapPost("/", Create).WithName("SatelliteDataEndpoints_Create").Produces<SatelliteData>(201).ProducesValidationProblem().RequireAuthorization("Write");
    }



    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var item = await db.SatelliteData.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    public static async Task<IResult> GetAll(AgroConectaDbContext db, RiskAnalysisService risk) => Results.Ok(await db.SatelliteData.OrderByDescending(s => s.CaptureDate).ToListAsync());

    public static async Task<IResult> Create(SatelliteDataRequest request, AgroConectaDbContext db, RiskAnalysisService risk)
    {
        var cropArea = await db.CropAreas.FindAsync(request.CropAreaId);
        if (cropArea is null) return Results.BadRequest("Talhão não encontrado.");

        var data = new SatelliteData
        {
            CropAreaId = request.CropAreaId,
            Source = request.Source,
            NdviAverage = request.NdviAverage,
            NdviMin = request.NdviMin,
            NdviMax = request.NdviMax,
            SurfaceTemperature = request.SurfaceTemperature,
            CloudCoverage = request.CloudCoverage,
            FireFocusNearby = request.FireFocusNearby,
            FireFocusDistanceKm = request.FireFocusDistanceKm,
            CaptureDate = request.CaptureDate
        };

        var latestCapture = await db.SatelliteData.Where(x => x.CropAreaId == cropArea.Id)
            .Select(x => (DateOnly?)x.CaptureDate).MaxAsync();
        var isCurrent = latestCapture is null || data.CaptureDate >= latestCapture;
        if (isCurrent) cropArea.Status = risk.CalculateCropStatus(data.NdviAverage, data.SurfaceTemperature, data.CloudCoverage, data.FireFocusNearby);

        var alert = risk.GenerateAlertIfNeeded(cropArea, data);
        if (isCurrent && alert is not null && !await db.ClimateAlerts.AnyAsync(a => a.CropAreaId == cropArea.Id
            && a.AlertType == alert.AlertType && a.Severity == alert.Severity && a.Status != AgroConecta.Api.Domain.Enums.AlertStatus.RESOLVED
            && a.Status != AgroConecta.Api.Domain.Enums.AlertStatus.IGNORED))
        {
            db.ClimateAlerts.Add(alert);
            db.Recommendations.Add(new Recommendation
            {
                ClimateAlert = alert,
                Priority = alert.Severity.ToString(),
                Message = "Verificar o talhão e considerar irrigação ou inspeção local conforme o nível de risco."
            });
        }

        db.SatelliteData.Add(data);
        await db.SaveChangesAsync();

        return Results.Created($"/api/satellite-data/{data.Id}", data);
    }
}
