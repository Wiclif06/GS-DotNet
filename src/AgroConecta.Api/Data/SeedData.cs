using AgroConecta.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Data;
public static class SeedData
{
    public static async Task InsertAsync(AgroConectaDbContext db, IConfiguration config, IPasswordHasher<User> hasher)
    {
        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (password.Length < 10) throw new InvalidOperationException("Seed:AdminPassword deve ter pelo menos 10 caracteres.");
        var existing = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            // An explicit seed may activate an account imported from the passwordless legacy schema.
            if (string.IsNullOrEmpty(existing.PasswordHash))
            {
                existing.PasswordHash = hasher.HashPassword(existing, password); existing.Role = "ADMIN";
                await db.SaveChangesAsync();
            }
            return;
        }
        var user = new User { Name = "Administrador AgroConecta", Email = email, Role = "ADMIN" };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user); await db.SaveChangesAsync();
    }
}
