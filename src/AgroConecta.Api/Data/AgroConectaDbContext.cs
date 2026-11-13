using AgroConecta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroConecta.Api.Data;

public class AgroConectaDbContext(DbContextOptions<AgroConectaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<CropArea> CropAreas => Set<CropArea>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<SatelliteData> SatelliteData => Set<SatelliteData>();
    public DbSet<ClimateAlert> ClimateAlerts => Set<ClimateAlert>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(x => x.Email).IsUnique();
        model.Entity<User>().Property(x => x.PasswordHash).HasDefaultValue("");
        model.Entity<Farm>().HasOne(x => x.User).WithMany(x => x.Farms).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<CropArea>().HasOne(x => x.Farm).WithMany(x => x.CropAreas).HasForeignKey(x => x.FarmId);
        model.Entity<Sensor>().HasOne(x => x.CropArea).WithMany(x => x.Sensors).HasForeignKey(x => x.CropAreaId);
        model.Entity<SensorReading>().HasOne(x => x.Sensor).WithMany(x => x.Readings).HasForeignKey(x => x.SensorId);
        model.Entity<SatelliteData>().HasOne(x => x.CropArea).WithMany(x => x.SatelliteData).HasForeignKey(x => x.CropAreaId);
        model.Entity<ClimateAlert>().HasOne(x => x.CropArea).WithMany(x => x.ClimateAlerts).HasForeignKey(x => x.CropAreaId);
        model.Entity<Recommendation>().HasOne(x => x.ClimateAlert).WithMany(x => x.Recommendations).HasForeignKey(x => x.ClimateAlertId);
        model.Entity<IdempotencyRecord>().HasKey(x => x.Key);
        model.Entity<IdempotencyRecord>().Property(x => x.Key).HasMaxLength(64);
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            var (precision, scale) = prop.Name switch
            {
                "Latitude" or "Longitude" => (10, 6),
                "TotalArea" or "AreaSize" => (12, 2),
                "FireFocusDistanceKm" => (8, 2),
                "Temperature" or "SurfaceTemperature" => (6, 2),
                _ => (5, 2)
            };
            prop.SetPrecision(precision); prop.SetScale(scale);
        }
    }
}
