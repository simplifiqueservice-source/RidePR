using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RidePR.Api.Hubs;
using RidePR.Api.Middleware;
using RidePR.Api.Services;
using RidePR.Application.Interfaces;
using RidePR.Application.Services;
using RidePR.Application.Settings;
using RidePR.Infrastructure.Authentication;
using RidePR.Infrastructure.Data;
using RidePR.Infrastructure.Dispatch;
using RidePR.Infrastructure.Maps;
using RidePR.Infrastructure.Payments;
using RidePR.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ridepr-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("TestClients", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite: Bearer SEU_TOKEN"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSignalR();

builder.Services.Configure<MapsOptions>(builder.Configuration.GetSection("Maps"));

var redisConnection = builder.Configuration.GetConnectionString("Redis");

if (string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "RidePR:";
    });
}

builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IAdminPanelRepository, AdminPanelRepository>();
builder.Services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
builder.Services.AddScoped<IFareSettingsRepository, FareSettingsRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IMapsCache, DistributedMapsCache>();
builder.Services.AddScoped<IDispatchQueue, RedisDispatchQueue>();
builder.Services.AddScoped<IDispatchNotifier, SignalRDispatchNotifier>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminPanelService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<PassengerService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DriverLocationService>();
builder.Services.AddScoped<DispatchService>();
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<FareCalculatorService>();
builder.Services.AddHostedService<DispatchTimeoutWorker>();

builder.Services.AddHttpClient<IMapProvider, OpenStreetMapProvider>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<MapsOptions>>()
        .Value
        .OpenStreetMap;

    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 10 : options.TimeoutSeconds);
});

builder.Services.AddHttpClient<IMapProvider, GoogleMapsProvider>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<MapsOptions>>()
        .Value
        .Google;

    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 10 : options.TimeoutSeconds);
});
builder.Services.AddScoped<RouteService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) &&
                path.StartsWithSegments("/driverHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();

var app = builder.Build();
var adminPanelPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend-admin"));

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
if (Directory.Exists(adminPanelPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminPanelPath),
        RequestPath = "/painel"
    });
}
app.UseCors("TestClients");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DriverHub>("/driverHub");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/painel", () => Results.Redirect("/painel/index.html"));
app.MapGet("/app", () => Results.Redirect("/painel/index.html"));

app.Run();
