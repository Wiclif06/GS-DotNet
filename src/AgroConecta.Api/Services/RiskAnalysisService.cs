using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Domain.Enums;

namespace AgroConecta.Api.Services;

public class RiskAnalysisService
{
    public CropStatus CalculateCropStatus(decimal ndviAverage, decimal surfaceTemperature, decimal cloudCoverage, bool fireFocusNearby)
    {
        if (fireFocusNearby && surfaceTemperature >= 34) return CropStatus.CRITICAL;
        if (cloudCoverage > 70) return CropStatus.LOW_RELIABILITY;
        if (ndviAverage < 0.35M && surfaceTemperature >= 34) return CropStatus.DROUGHT_RISK;
        if (ndviAverage < 0.45M) return CropStatus.ATTENTION;

        return CropStatus.NORMAL;
    }

    public ClimateAlert? GenerateAlertIfNeeded(CropArea cropArea, SatelliteData satelliteData)
    {
        var status = CalculateCropStatus(
            satelliteData.NdviAverage,
            satelliteData.SurfaceTemperature,
            satelliteData.CloudCoverage,
            satelliteData.FireFocusNearby
        );

        if (status is CropStatus.NORMAL or CropStatus.LOW_RELIABILITY)
            return null;

        var alertType = status switch
        {
            CropStatus.CRITICAL => "FIRE_RISK",
            CropStatus.DROUGHT_RISK => "DROUGHT_RISK",
            _ => "VEGETATION_STRESS"
        };

        var severity = status switch
        {
            CropStatus.CRITICAL => AlertSeverity.CRITICAL,
            CropStatus.DROUGHT_RISK => AlertSeverity.HIGH,
            _ => AlertSeverity.MEDIUM
        };

        return new ClimateAlert
        {
            CropAreaId = cropArea.Id,
            AlertType = alertType,
            Severity = severity,
            Title = status == CropStatus.CRITICAL ? "Risco crítico detectado" : "Atenção no talhão",
            Description = $"O talhão {cropArea.Name} apresentou NDVI {satelliteData.NdviAverage}, temperatura {satelliteData.SurfaceTemperature}°C e cobertura de nuvens {satelliteData.CloudCoverage}%.",
            Status = AlertStatus.OPEN
        };
    }
}
