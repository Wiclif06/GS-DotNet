using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class SatelliteData
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Source { get; set; } = "SIMULATED";

    public decimal NdviAverage { get; set; }
    public decimal NdviMin { get; set; }
    public decimal NdviMax { get; set; }
    public decimal SurfaceTemperature { get; set; }
    public decimal CloudCoverage { get; set; }
    public bool FireFocusNearby { get; set; }
    public decimal? FireFocusDistanceKm { get; set; }
    public DateOnly CaptureDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CropAreaId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CropArea? CropArea { get; set; }
}
