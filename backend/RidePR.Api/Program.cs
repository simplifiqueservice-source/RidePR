using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Application.Services;
using RidePR.Infrastructure.Data;
using RidePR.Infrastructure.Repositories;
using RidePR.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// =====================
// Controllers + Swagger
// =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// =====================
// Dependency Injection
// =====================
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<DriverLocationService>();
builder.Services.AddScoped<DispatchService>();
builder.Services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
builder.Services.AddScoped<IFareSettingsRepository, FareSettingsRepository>();
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<FareCalculatorService>();
builder.Services.AddHttpClient<RouteService>();

// =====================
// Database (PostgreSQL)
// =====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()));

// =====================
// Health Checks
// =====================
builder.Services.AddHealthChecks();

var app = builder.Build();

// =====================
// Middleware
// =====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<DriverHub>("/driverHub");

app.MapHealthChecks("/health");

app.Run();