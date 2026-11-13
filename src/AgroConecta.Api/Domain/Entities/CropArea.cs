using AgroConecta.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class CropArea
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CropType { get; set; } = string.Empty;

    public decimal AreaSize { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public CropStatus Status { get; set; } = CropStatus.NORMAL;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int FarmId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Farm? Farm { get; set; }

    public List<Sensor> Sensors { get; set; } = new();
    public List<SatelliteData> SatelliteData { get; set; } = new();
    public List<ClimateAlert> ClimateAlerts { get; set; } = new();
}
