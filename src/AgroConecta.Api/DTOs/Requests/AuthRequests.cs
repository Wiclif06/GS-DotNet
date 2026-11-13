using System.ComponentModel.DataAnnotations;
namespace AgroConecta.Api.DTOs.Requests;

public record RegisterRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, EmailAddress, StringLength(160)] string Email,
    [property: Required, StringLength(128, MinimumLength = 10)] string Password);
public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);
