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
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<PassengerHistory> PassengerHistory => Set<PassengerHistory>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<FareSettings> FareSettings => Set<FareSettings>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentSplit> PaymentSplits => Set<PaymentSplit>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

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

            entity.HasOne(x => x.Passenger)
                .WithOne(x => x.User)
                .HasForeignKey<Passenger>(x => x.UserId);

            entity.Property(x => x.AdminType)
                .HasConversion<int?>();

            entity.HasIndex(x => x.BranchId);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.City)
                .HasMaxLength(100);

            entity.Property(x => x.State)
                .HasMaxLength(2);

            entity.Property(x => x.Address)
                .HasMaxLength(300);

            entity.Property(x => x.Phone)
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("Drivers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasIndex(x => x.BranchId);

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

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.ToTable("Passengers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasIndex(x => x.BranchId);

            entity.HasIndex(x => x.Cpf)
                .IsUnique();

            entity.Property(x => x.Cpf)
                .HasMaxLength(14)
                .IsRequired();

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

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PassengerHistory>(entity =>
        {
            entity.ToTable("PassengerHistory");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.PassengerId);

            entity.Property(x => x.Type)
                .HasConversion<int>();

            entity.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasOne(x => x.Passenger)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.PassengerId);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("Trips");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.BranchId);
            entity.HasIndex(x => x.PassengerId);
            entity.HasIndex(x => x.DriverId);
            entity.HasIndex(x => x.Status);

            entity.Property(x => x.EstimatedDistanceKm).HasPrecision(18, 2);
            entity.Property(x => x.EstimatedDurationMinutes).HasPrecision(18, 2);
            entity.Property(x => x.ActualDistanceKm).HasPrecision(18, 2);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<int>();

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FareSettings>(entity =>
        {
            entity.ToTable("FareSettings");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.BranchId, x.Active });

            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.BaseFare).HasPrecision(18, 2);
            entity.Property(x => x.MinimumFare).HasPrecision(18, 2);
            entity.Property(x => x.IncludedDistanceKm).HasPrecision(18, 2);
            entity.Property(x => x.PricePerKm).HasPrecision(18, 2);
            entity.Property(x => x.PricePerMinute).HasPrecision(18, 2);
            entity.Property(x => x.WaitingMinutePrice).HasPrecision(18, 2);
            entity.Property(x => x.CancellationFee).HasPrecision(18, 2);
            entity.Property(x => x.PlatformCommission).HasPrecision(18, 2);
            entity.Property(x => x.DynamicMultiplier).HasPrecision(18, 2);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DriverLocation>(entity =>
        {
            entity.Property(x => x.Position)
                .HasColumnType("geometry(Point,4326)");

            entity.HasIndex(x => x.DriverId)
                .IsUnique();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TripId)
                .IsUnique();

            entity.HasIndex(x => x.PassengerId);
            entity.HasIndex(x => x.DriverId);
            entity.HasIndex(x => x.Status);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.RefundedAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.Provider)
                .HasMaxLength(50);

            entity.Property(x => x.ProviderPaymentId)
                .HasMaxLength(150);

            entity.Property(x => x.PixQrCode)
                .HasMaxLength(1000);

            entity.Property(x => x.PixCopyPaste)
                .HasMaxLength(1000);

            entity.Property(x => x.CardLast4)
                .HasMaxLength(4);

            entity.Property(x => x.CardBrand)
                .HasMaxLength(50);

            entity.Property(x => x.FailureReason)
                .HasMaxLength(500);

            entity.Property(x => x.Method)
                .HasConversion<int>();

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasOne(x => x.Trip)
                .WithMany()
                .HasForeignKey(x => x.TripId);

            entity.HasOne(x => x.Passenger)
                .WithMany()
                .HasForeignKey(x => x.PassengerId);

            entity.HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId);
        });

        modelBuilder.Entity<PaymentSplit>(entity =>
        {
            entity.ToTable("PaymentSplits");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RecipientType)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Percentage)
                .HasPrecision(5, 2);

            entity.HasOne(x => x.Payment)
                .WithMany(x => x.Splits)
                .HasForeignKey(x => x.PaymentId);

            entity.HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId);
        });

        modelBuilder.Entity<PaymentRefund>(entity =>
        {
            entity.ToTable("PaymentRefunds");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ProviderRefundId)
                .HasMaxLength(150);

            entity.Property(x => x.Status)
                .HasConversion<int>();

            entity.HasOne(x => x.Payment)
                .WithMany(x => x.Refunds)
                .HasForeignKey(x => x.PaymentId);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallets");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.Property(x => x.Balance)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.ToTable("WalletTransactions");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.WalletId);
            entity.HasIndex(x => x.PaymentId);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.BalanceAfter)
                .HasPrecision(18, 2);

            entity.Property(x => x.Description)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.Type)
                .HasConversion<int>();

            entity.HasOne(x => x.Wallet)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.WalletId);

            entity.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId);
        });
    }
}
