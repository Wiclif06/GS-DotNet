using AgroConecta.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class ClimateAlert
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string AlertType { get; set; } = "VEGETATION_STRESS";

    public AlertSeverity Severity { get; set; } = AlertSeverity.LOW;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public AlertStatus Status { get; set; } = AlertStatus.OPEN;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public int CropAreaId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CropArea? CropArea { get; set; }

    public List<Recommendation> Recommendations { get; set; } = new();
}
