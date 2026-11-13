using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.Domain.Enums;
using AgroConecta.Api.Services;
using Xunit;
namespace AgroConecta.Api.Tests;
public class RiskAnalysisTests
{
    [Theory]
    [InlineData(0.8, 25, 10, false, CropStatus.NORMAL)]
    [InlineData(0.44, 25, 10, false, CropStatus.ATTENTION)]
    [InlineData(0.34, 34, 10, false, CropStatus.DROUGHT_RISK)]
    [InlineData(0.34, 33.9, 10, false, CropStatus.ATTENTION)]
    [InlineData(0.35, 34, 10, false, CropStatus.ATTENTION)]
    [InlineData(0.45, 25, 70, false, CropStatus.NORMAL)]
    [InlineData(0.8, 25, 71, false, CropStatus.LOW_RELIABILITY)]
    [InlineData(0.8, 34, 90, true, CropStatus.CRITICAL)]
    public void Classifies_observations(decimal ndvi, decimal temperature, decimal clouds, bool fire, CropStatus expected)
        => Assert.Equal(expected, new RiskAnalysisService().CalculateCropStatus(ndvi, temperature, clouds, fire));
    [Fact]
    public void Generates_actionable_fire_alert()
    {
        var alert = new RiskAnalysisService().GenerateAlertIfNeeded(new CropArea { Id = 7, Name = "Talhão comunitário" },
            new SatelliteData { NdviAverage = .7m, SurfaceTemperature = 40, FireFocusNearby = true });
        Assert.NotNull(alert); Assert.Equal(7, alert.CropAreaId); Assert.Equal("FIRE_RISK", alert.AlertType);
        Assert.Equal(AlertSeverity.CRITICAL, alert.Severity);
    }
    [Fact]
    public void Low_confidence_does_not_generate_false_alert()
        => Assert.Null(new RiskAnalysisService().GenerateAlertIfNeeded(new CropArea(), new SatelliteData { CloudCoverage = 90 }));
}
