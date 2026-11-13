using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class UsersEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization("Admin").AddEndpointFilter<ValidationFilter>();
        group.MapGet("/", GetAll).WithName("UsersEndpoints_GetAll").Produces<User[]>();
        group.MapGet("/{id:int}", GetById).WithName("UsersEndpoints_GetById").Produces<User>();
        group.MapPost("/", Create).WithName("UsersEndpoints_Create").Produces<User>(201).ProducesValidationProblem().RequireAuthorization("Admin");
    }


    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.Users.OrderBy(u => u.Id).ToListAsync());

    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var user = await db.Users.Include(u => u.Farms).FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    public static async Task<IResult> Create(UserRequest request, AgroConectaDbContext db, Microsoft.AspNetCore.Identity.IPasswordHasher<User> hasher)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Results.Conflict(new { error = "E-mail já cadastrado." });
        if (request.Role is not ("ADMIN" or "PRODUCER" or "READER")) return Results.BadRequest("Perfil inválido.");

        var user = new User { Name = request.Name, Email = email, Role = request.Role };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.Created($"/api/users/{user.Id}", user);
    }
}
