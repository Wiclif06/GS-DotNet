namespace AgroConecta.Api.Domain.Entities;

// Persisted together with the business operation in a single transaction.
public class IdempotencyRecord
{
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public int StatusCode { get; set; }
    public string Body { get; set; } = "";
    public string ContentType { get; set; } = "application/json";
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
