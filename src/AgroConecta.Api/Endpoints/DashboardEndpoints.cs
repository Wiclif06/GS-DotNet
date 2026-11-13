using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Data;
using AgroConecta.Api.DTOs.Responses;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AgroConecta.Api.Domain.Enums;

namespace AgroConecta.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/", GetDashboard).WithName("DashboardEndpoints_GetDashboard").Produces<DashboardResponse>();
    }


    public static async Task<IResult> GetDashboard(AgroConectaDbContext db)
    {
        // SQLite cannot translate AVG(decimal); a nullable double projection works
        // on both providers and also handles an empty history in one query.
        var averageNdvi = (decimal)(await db.SatelliteData
            .Select(s => (double?)s.NdviAverage).AverageAsync() ?? 0d);

        return Results.Ok(new DashboardResponse(
            await db.Users.CountAsync(),
            await db.Farms.CountAsync(),
            await db.CropAreas.CountAsync(),
            await db.Sensors.CountAsync(),
            await db.SatelliteData.CountAsync(),
            await db.ClimateAlerts.CountAsync(a => a.Status == AlertStatus.OPEN),
            await db.Recommendations.CountAsync(),
            averageNdvi
        ));
    }
}
