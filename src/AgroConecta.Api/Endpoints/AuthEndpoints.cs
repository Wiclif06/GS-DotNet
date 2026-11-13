using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.DTOs.Responses;
using AgroConecta.Api.Infrastructure;
using AgroConecta.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;
public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AddEndpointFilter<ValidationFilter>().RequireRateLimiting("login");
        group.MapPost("/register", async (RegisterRequest request, AgroConectaDbContext db, IPasswordHasher<User> hasher) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email)) return Results.Conflict(new { error = "E-mail já cadastrado." });
            var user = new User { Name = request.Name.Trim(), Email = email, Role = "PRODUCER" };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user); await db.SaveChangesAsync();
            return Results.Created("/api/auth/me", new { user.Id, user.Name, user.Email, user.Role });
        }).Produces<UserSummary>(201).ProducesValidationProblem().ProducesProblem(409);
        group.MapPost("/login", async (LoginRequest request, AgroConectaDbContext db, IPasswordHasher<User> hasher, TokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null || string.IsNullOrEmpty(user.PasswordHash) || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Results.Unauthorized();
            return Results.Ok(tokens.Create(user));
        }).Produces<TokenResponse>(200).Produces(401).ProducesValidationProblem();
        group.MapGet("/me", async (System.Security.Claims.ClaimsPrincipal principal, AgroConectaDbContext db) =>
        {
            var id = int.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var user = await db.Users.FindAsync(id);
            return user is null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.Name, user.Email, user.Role });
        }).RequireAuthorization().Produces<UserSummary>(200).Produces(401);
    }
}
