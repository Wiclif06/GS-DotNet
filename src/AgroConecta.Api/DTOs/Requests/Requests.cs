using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.DTOs.Requests;

public record UserRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, EmailAddress, StringLength(160)] string Email,
    [property: Required, StringLength(40)] string Role,
    [property: Required, StringLength(128, MinimumLength = 10)] string Password
);

public record FarmRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(120)] string OwnerName,
    [property: Required, StringLength(80)] string City,
    [property: Required, StringLength(2, MinimumLength = 2)] string State,
    [property: Range(0.1, 999999)] decimal TotalArea,
    [property: Range(1, int.MaxValue)] int? UserId
);

public record CropAreaRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(80)] string CropType,
    [property: Range(0.1, 999999)] decimal AreaSize,
    [property: Range(-90, 90)] decimal Latitude,
    [property: Range(-180, 180)] decimal Longitude,
    [property: Range(1, int.MaxValue)] int FarmId
);

public record SensorRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, StringLength(60)] string SensorType,
    [property: Range(1, int.MaxValue)] int CropAreaId
);

public record SensorReadingRequest(
    [property: Range(1, int.MaxValue)] int SensorId,
    [property: Range(-30, 80)] decimal Temperature,
    [property: Range(0, 100)] decimal AirHumidity,
    [property: Range(0, 100)] decimal SoilHumidity,
    bool ManualAlert
);

public record SatelliteDataRequest(
    [property: Range(1, int.MaxValue)] int CropAreaId,
    [property: Required, StringLength(80)] string Source,
    [property: Range(-1, 1)] decimal NdviAverage,
    [property: Range(-1, 1)] decimal NdviMin,
    [property: Range(-1, 1)] decimal NdviMax,
    [property: Range(-50, 90)] decimal SurfaceTemperature,
    [property: Range(0, 100)] decimal CloudCoverage,
    bool FireFocusNearby,
    [property: Range(0, 999999)] decimal? FireFocusDistanceKm,
    DateOnly CaptureDate
);

public record ClimateAlertRequest(
    [property: Range(1, int.MaxValue)] int CropAreaId,
    [property: Required, StringLength(60)] string AlertType,
    [property: Required, StringLength(160)] string Title,
    [property: Required, StringLength(500)] string Description,
    [property: Required] string Severity
);

public record RecommendationRequest(
    [property: Range(1, int.MaxValue)] int ClimateAlertId,
    [property: Required, StringLength(500)] string Message,
    [property: Required, StringLength(30)] string Priority
);
