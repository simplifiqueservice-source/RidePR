using Microsoft.EntityFrameworkCore;
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

            entity.HasOne(x => x.Driver)
                .WithOne(x => x.User)
                .HasForeignKey<Driver>(x => x.UserId);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("Drivers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasIndex(x => x.Cpf)
                .IsUnique();

            entity.Property(x => x.Cpf)
                .HasMaxLength(14)
                .IsRequired();

            entity.Property(x => x.Rg)
                .HasMaxLength(20);

            entity.Property(x => x.Phone)
                .HasMaxLength(20);

            entity.Property(x => x.EmergencyPhone)
                .HasMaxLength(20);

            entity.Property(x => x.Address)
                .HasMaxLength(300);

            entity.Property(x => x.City)
                .HasMaxLength(100);

            entity.Property(x => x.State)
                .HasMaxLength(2);

            entity.Property(x => x.ZipCode)
                .HasMaxLength(10);

            entity.Property(x => x.CnhNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(x => x.CnhNumber)
                .IsUnique();

            entity.Property(x => x.CnhCategory)
                .HasMaxLength(5);

            entity.Property(x => x.RejectReason)
                .HasMaxLength(500);

            entity.Property(x => x.PhotoUrl)
                .HasMaxLength(500);

            entity.Property(x => x.CnhFrontUrl)
                .HasMaxLength(500);

            entity.Property(x => x.CnhBackUrl)
                .HasMaxLength(500);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.Property(x => x.ApprovalStatus)
                .HasConversion<int>();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.DriverId);

            entity.HasIndex(x => x.Plate)
                .IsUnique();

            entity.Property(x => x.Plate)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(x => x.Model)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Brand)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Color)
                .HasMaxLength(50);

            entity.Property(x => x.Renavam)
                .HasMaxLength(20);

            entity.Property(x => x.Chassis)
                .HasMaxLength(30);

            entity.Property(x => x.PhotoUrl)
                .HasMaxLength(500);

            entity.Property(x => x.RegistrationDocumentUrl)
                .HasMaxLength(500);

            entity.HasOne(x => x.Driver)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.DriverId);
        });

        modelBuilder.Entity<DriverLocation>(entity =>
        {
            entity.Property(x => x.Position)
                .HasColumnType("geometry(Point,4326)");

            entity.HasIndex(x => x.DriverId)
                .IsUnique();
        });
    }
}
