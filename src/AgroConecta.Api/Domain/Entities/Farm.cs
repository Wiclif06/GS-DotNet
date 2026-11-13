using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class Farm
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string OwnerName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(2)]
    public string State { get; set; } = string.Empty;

    public decimal TotalArea { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public User? User { get; set; }

    public List<CropArea> CropAreas { get; set; } = new();
}
