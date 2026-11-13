namespace AgroConecta.Api.DTOs.Responses;

public record UserSummary(int Id, string Name, string Email, string Role);
public record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UserSummary User);
