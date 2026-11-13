using System.ComponentModel.DataAnnotations;

namespace AgroConecta.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Role { get; set; } = "PRODUCER";

    [System.Text.Json.Serialization.JsonIgnore]
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Farm> Farms { get; set; } = new();
}
