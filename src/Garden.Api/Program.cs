using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.Infrastructure.Configuration;
using Garden.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddGardenInfrastructure(builder.Configuration);
builder.Services.AddGardenEngine();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
    await db.Database.MigrateAsync();

    var initializer = scope.ServiceProvider.GetRequiredService<WorldInitializer>();
    var coordinator = scope.ServiceProvider.GetRequiredService<SimulationCoordinator>();
    var scheduler = scope.ServiceProvider.GetRequiredService<Garden.Core.Interfaces.ISimulationScheduler>();

    scheduler.Register(scope.ServiceProvider.GetRequiredService<WeatherSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<SeasonSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<HydrologySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<ResourceSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EcologySystem>());

    initializer.Initialize(width: 100, height: 100, seed: 42);
}

app.UseCors();
app.MapControllers();

app.Run();
