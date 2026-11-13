namespace AgroConecta.Api.DTOs.Responses;

public record DashboardResponse(
    int Users,
    int Farms,
    int CropAreas,
    int Sensors,
    int SatelliteRecords,
    int OpenAlerts,
    int Recommendations,
    decimal AverageNdvi
);
