using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class GovernanceService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<GovernanceService> _logger;

    private static readonly string[] GovernmentProgression =
        ["Informal Community", "Council", "Village Chief", "Elder Assembly"];

    public GovernanceService(WorldState worldState, IEventBus eventBus, ILogger<GovernanceService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateGovernance(Settlement settlement, long tick)
    {
        var population = settlement.MemberIds.Count;
        var currentIdx = Array.IndexOf(GovernmentProgression, settlement.GovernmentType);
        if (currentIdx < 0) currentIdx = 0;

        var targetIdx = DetermineTargetGovernment(population);
        if (targetIdx != currentIdx)
        {
            var previous = settlement.GovernmentType;
            settlement.GovernmentType = GovernmentProgression[targetIdx];

            _eventBus.Publish(new GovernmentFormedEvent
            {
                Tick = tick,
                SettlementId = settlement.Id,
                SettlementName = settlement.Name,
                GovernmentType = settlement.GovernmentType,
                PreviousGovernmentType = previous
            });

            _logger.LogInformation("{Settlement} evolved from {Prev} to {New} government",
                settlement.Name, previous, settlement.GovernmentType);
        }
    }

    private static int DetermineTargetGovernment(int population)
    {
        if (population >= 20) return 3;
        if (population >= 10) return 2;
        if (population >= 5) return 1;
        return 0;
    }
}
