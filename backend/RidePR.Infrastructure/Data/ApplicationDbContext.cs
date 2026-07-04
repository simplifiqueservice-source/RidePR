using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RidePR.Domain.Entities;

namespace RidePR.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<FareSettings> FareSettings => Set<FareSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DriverLocation>(entity =>
        {
            entity.Property(x => x.Position)
                .HasColumnType("geometry(Point,4326)");

            entity.HasIndex(x => x.DriverId)
                .IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired();
        });
    }
}