using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class MigrationService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(WorldState worldState, IEventBus eventBus, ILogger<MigrationService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateMigration(long tick)
    {
        var citizens = _worldState.Citizens.Where(c => c.IsAlive && c.HomeSettlementId != null).ToList();

        foreach (var citizen in citizens)
        {
            var reason = ShouldMigrate(citizen);
            if (reason == null) continue;

            var currentHome = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            if (currentHome == null) continue;

            var destination = FindDestination(citizen, currentHome);
            if (destination == null) continue;

            ExecuteMigration(citizen, currentHome, destination, reason, tick);
        }
    }

    private static string? ShouldMigrate(Citizen citizen)
    {
        if (citizen.Needs.Hunger >= 70) return "Food shortage";
        if (citizen.Needs.Thirst >= 70) return "Water shortage";
        if (citizen.Needs.Health <= 30) return "Poor health";
        if (citizen.Personality.Curiosity > 70 && citizen.Age < 40 && System.Random.Shared.NextDouble() < 0.02)
            return "Desire for exploration";
        if (citizen.Needs.Warmth <= 20) return "Harsh climate";

        return null;
    }

    private Settlement? FindDestination(Citizen citizen, Settlement currentHome)
    {
        var candidates = _worldState.Settlements
            .Where(s => s.Id != currentHome.Id)
            .Select(s => new
            {
                Settlement = s,
                Score = CalculateAttractiveness(s, citizen, currentHome)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        return candidates.FirstOrDefault()?.Settlement;
    }

    private static double CalculateAttractiveness(Settlement target, Citizen citizen, Settlement currentHome)
    {
        var score = 0.0;
        var foodScore = target.Storage.GetQuantity("Food") / Math.Max(1, target.MemberIds.Count);
        var currentFoodScore = currentHome.Storage.GetQuantity("Food") / Math.Max(1, currentHome.MemberIds.Count);
        score += (foodScore - currentFoodScore) * 0.5;

        if (target.HasAvailableHousing) score += 10;
        if (target.LeaderId != null) score += 5;
        if (target.CompletedBuildings > currentHome.CompletedBuildings) score += 3;
        if (target.MemberIds.Count < currentHome.MemberIds.Count) score += 2;

        var dist = Math.Abs(target.TileX - citizen.TileX) + Math.Abs(target.TileY - citizen.TileY);
        score -= dist * 0.5;

        if (citizen.Personality.Introversion > 60) score -= 5;
        if (citizen.Personality.Curiosity > 60) score += dist * 0.2;

        return score;
    }

    private void ExecuteMigration(Citizen citizen, Settlement from, Settlement to, string reason, long tick)
    {
        from.MemberIds.Remove(citizen.Id);
        from.Population = from.MemberIds.Count;

        citizen.HomeSettlementId = to.Id;
        citizen.TileX = to.TileX + System.Random.Shared.Next(-3, 4);
        citizen.TileY = to.TileY + System.Random.Shared.Next(-3, 4);
        citizen.CurrentGoal = "Migrating";
        citizen.CurrentActivity = "Migrating";

        var name = $"{citizen.FirstName} {citizen.LastName}";

        _eventBus.Publish(new MigrationStartedEvent
        {
            Tick = tick,
            CitizenId = citizen.Id,
            CitizenName = name,
            FromSettlementId = from.Id,
            FromSettlementName = from.Name,
            Reason = reason,
            FromX = citizen.TileX,
            FromY = citizen.TileY
        });

        to.MemberIds.Add(citizen.Id);
        to.Population = to.MemberIds.Count;

        _eventBus.Publish(new MigrationCompletedEvent
        {
            Tick = tick,
            CitizenId = citizen.Id,
            CitizenName = name,
            ToSettlementId = to.Id,
            ToSettlementName = to.Name,
            ToX = citizen.TileX,
            ToY = citizen.TileY
        });

        _logger.LogInformation("{Name} migrated from {From} to {To} due to {Reason}",
            name, from.Name, to.Name, reason);
    }
}
