using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class LeadershipService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LeadershipService> _logger;

    public LeadershipService(WorldState worldState, IEventBus eventBus, ILogger<LeadershipService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateLeadership(Settlement settlement, long tick)
    {
        if (settlement.MemberIds.Count < 3) return;

        var members = _worldState.Citizens
            .Where(c => settlement.MemberIds.Contains(c.Id) && c.IsAlive)
            .ToList();

        if (members.Count == 0) return;

        var bestCandidate = members
            .OrderByDescending(c => GetLeadershipScore(c))
            .First()!;

        if (settlement.LeaderId == null || !settlement.LeaderId.Equals(bestCandidate.Id))
        {
            var previousLeaderName = settlement.LeaderName;
            var previousLeader = settlement.LeaderId != null
                ? _worldState.Citizens.FirstOrDefault(c => c.Id == settlement.LeaderId)
                : null;

            settlement.LeaderId = bestCandidate.Id;
            settlement.LeaderName = $"{bestCandidate.FirstName} {bestCandidate.LastName}";

            _eventBus.Publish(new LeaderElectedEvent
            {
                Tick = tick,
                CitizenId = bestCandidate.Id,
                CitizenName = $"{bestCandidate.FirstName} {bestCandidate.LastName}",
                SettlementId = settlement.Id,
                SettlementName = settlement.Name,
                PreviousLeaderName = previousLeader != null
                    ? $"{previousLeader.FirstName} {previousLeader.LastName}"
                    : "None",
                ContributionScore = bestCandidate.ContributionScore
            });

            _logger.LogInformation("{Leader} elected as leader of {Settlement}",
                $"{bestCandidate.FirstName} {bestCandidate.LastName}", settlement.Name);
        }
    }

    private static double GetLeadershipScore(Citizen citizen)
    {
        return citizen.ContributionScore * 2.0
            + citizen.Reputation * 1.0
            + citizen.Attributes.Intelligence * 0.5
            + citizen.Personality.Compassion * 0.3
            + citizen.Personality.Diligence * 0.4
            + citizen.Age * 0.1;
    }

    public void AwardContribution(Citizen citizen, double amount)
    {
        citizen.ContributionScore += amount;
        citizen.Reputation = Math.Min(100, citizen.Reputation + amount * 0.1);
    }
}
