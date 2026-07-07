using Garden.Engine.Services;
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
}

app.UseCors();
app.MapControllers();

app.Run();
