using Garden.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly GardenDbContext _db;

    public HealthController(GardenDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbConnected = false;
        try
        {
            dbConnected = await _db.Database.CanConnectAsync();
        }
        catch
        {
            // Database not available
        }

        return Ok(new
        {
            Status = "Healthy",
            ApiVersion = "0.1.0",
            Database = dbConnected ? "Connected" : "Disconnected",
            Timestamp = DateTime.UtcNow
        });
    }
}
