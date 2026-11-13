using AgroConecta.Api.Data;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Requests;
using AgroConecta.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Endpoints;

public static class FarmsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/farms").WithTags("Farms").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapGet("/", GetAll).WithName("FarmsEndpoints_GetAll").Produces<Farm[]>();
        group.MapGet("/{id:int}", GetById).WithName("FarmsEndpoints_GetById").Produces<Farm>();
        group.MapPost("/", Create).WithName("FarmsEndpoints_Create").Produces<Farm>(201).ProducesValidationProblem().RequireAuthorization("Write");
        group.MapPut("/{id:int}", Update).WithName("FarmsEndpoints_Update").Produces(204).ProducesProblem(404).RequireAuthorization("Write");
        group.MapDelete("/{id:int}", Delete).WithName("FarmsEndpoints_Delete").Produces(204).ProducesProblem(404).RequireAuthorization("Write");
    }


    public static async Task<IResult> GetAll(AgroConectaDbContext db) => Results.Ok(await db.Farms.Include(f => f.CropAreas).OrderBy(f => f.Id).ToListAsync());

    public static async Task<IResult> GetById(int id, AgroConectaDbContext db)
    {
        var farm = await db.Farms.Include(f => f.CropAreas).FirstOrDefaultAsync(f => f.Id == id);
        return farm is null ? Results.NotFound() : Results.Ok(farm);
    }

    public static async Task<IResult> Create(FarmRequest request, AgroConectaDbContext db)
    {
        if (request.UserId is not null && !await db.Users.AnyAsync(u => u.Id == request.UserId))
            return Results.BadRequest("Usuário não encontrado.");

        var farm = new Farm
        {
            Name = request.Name,
            OwnerName = request.OwnerName,
            City = request.City,
            State = request.State.ToUpper(),
            TotalArea = request.TotalArea,
            UserId = request.UserId
        };

        db.Farms.Add(farm);
        await db.SaveChangesAsync();
        return Results.Created($"/api/farms/{farm.Id}", farm);
    }

    public static async Task<IResult> Update(int id, FarmRequest request, AgroConectaDbContext db)
    {
        var farm = await db.Farms.FindAsync(id);
        if (farm is null) return Results.NotFound();

        if (request.UserId is not null && !await db.Users.AnyAsync(u => u.Id == request.UserId))
            return Results.BadRequest("Usuário não encontrado.");

        farm.Name = request.Name;
        farm.OwnerName = request.OwnerName;
        farm.City = request.City;
        farm.State = request.State.ToUpper();
        farm.TotalArea = request.TotalArea;
        farm.UserId = request.UserId;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    public static async Task<IResult> Delete(int id, AgroConectaDbContext db)
    {
        var farm = await db.Farms.FindAsync(id);
        if (farm is null) return Results.NotFound();

        if (await db.CropAreas.AnyAsync(c => c.FarmId == id))
            return Results.Conflict(new { error = "Remova os talhões antes de excluir a fazenda." });

        db.Farms.Remove(farm);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
