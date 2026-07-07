using Garden.Infrastructure.Persistence;
using Garden.World.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Garden.Infrastructure.Services;

public class WorldPersistenceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorldState _worldState;
    private readonly ILogger<WorldPersistenceService> _logger;
    private readonly TimeSpan _saveInterval = TimeSpan.FromSeconds(30);

    public WorldPersistenceService(
        IServiceScopeFactory scopeFactory,
        WorldState worldState,
        ILogger<WorldPersistenceService> logger)
    {
        _scopeFactory = scopeFactory;
        _worldState = worldState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("World persistence service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_saveInterval, stoppingToken);
                await SaveSnapshot(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _logger.LogInformation("World persistence service shutting down — performing final save");
            await SaveSnapshot(CancellationToken.None);
        }
    }

    private async Task SaveSnapshot(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

            var existingCitizenIds = await db.Citizens
                .IgnoreAutoIncludes()
                .Select(c => c.Id)
                .ToHashSetAsync(ct);

            var existingSettlementIds = await db.Settlements
                .IgnoreAutoIncludes()
                .Select(s => s.Id)
                .ToHashSetAsync(ct);

            foreach (var citizen in _worldState.Citizens)
            {
                if (existingCitizenIds.Contains(citizen.Id))
                {
                    db.Attach(citizen);
                    db.Entry(citizen).State = EntityState.Modified;
                }
                else
                {
                    db.Citizens.Add(citizen);
                }
            }

            foreach (var settlement in _worldState.Settlements)
            {
                if (existingSettlementIds.Contains(settlement.Id))
                {
                    db.Attach(settlement);
                    db.Entry(settlement).State = EntityState.Modified;
                }
                else
                {
                    db.Settlements.Add(settlement);
                }
            }

            await db.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Persisted {CitizenCount} citizens, {SettlementCount} settlements",
                _worldState.Citizens.Count,
                _worldState.Settlements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist world snapshot");
        }
    }
}
