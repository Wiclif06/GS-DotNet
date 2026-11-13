using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class Recommendation
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Priority { get; set; } = "MEDIUM";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ClimateAlertId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ClimateAlert? ClimateAlert { get; set; }
}
