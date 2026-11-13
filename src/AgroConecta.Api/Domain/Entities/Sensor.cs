using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class Sensor
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string SensorType { get; set; } = "SOIL_MOISTURE";

    [Required, MaxLength(30)]
    public string Status { get; set; } = "ONLINE";

    public DateTime InstallationDate { get; set; } = DateTime.UtcNow;

    public int CropAreaId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CropArea? CropArea { get; set; }

    public List<SensorReading> Readings { get; set; } = new();
}
