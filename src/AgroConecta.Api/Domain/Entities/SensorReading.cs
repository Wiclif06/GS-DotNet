namespace AgroConecta.Api.Domain.Entities;

public class SensorReading
{
    public int Id { get; set; }

    public decimal Temperature { get; set; }
    public decimal AirHumidity { get; set; }
    public decimal SoilHumidity { get; set; }
    public bool ManualAlert { get; set; }
    public DateTime ReadingDate { get; set; } = DateTime.UtcNow;

    public int SensorId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Sensor? Sensor { get; set; }
}
