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

    // Index-aligned with GovernmentProgression. TG-580 lists 6 authority
    // sources; only these 3 are reachable through population-driven
    // government evolution today (see Settlement.AuthoritySource).
    private static readonly string[] AuthorityProgression =
        ["Competence", "Election", "Tradition", "Tradition"];

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
            settlement.LastGovernmentChangeTick = tick;

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

        // Kept unconditional (not just inside the transition branch above) so
        // it self-heals for settlements that reached their current government
        // tier before AuthoritySource existed (e.g. resumed saves whose
        // AuthoritySource column defaulted to "" via the EF migration) -
        // AuthoritySource should always reflect the settlement's CURRENT
        // government type, the same way Legitimacy is recomputed every call.
        settlement.AuthoritySource = AuthorityProgression[targetIdx];
        settlement.Legitimacy = CalculateLegitimacy(settlement, tick);
    }

    private static int DetermineTargetGovernment(int population)
    {
        if (population >= 20) return 3;
        if (population >= 10) return 2;
        if (population >= 5) return 1;
        return 0;
    }

    /// <summary>
    /// TG-580 names public trust, competence, justice, and stability as
    /// Legitimacy inputs but gives no formula. "Justice" is omitted (TG-590
    /// Law &amp; Justice is unimplemented). Competence and public trust come
    /// from the actual leader's real ContributionScore/Reputation rather
    /// than invented numbers; stability ramps from 0 to 100 over the ~500
    /// ticks (~20 in-game days) following the settlement's last government
    /// transition, so a settlement is least legitimate right after upheaval
    /// and gains legitimacy the longer its government holds.
    ///
    /// Exposed as a public breakdown (not just the combined total) so the
    /// Observatory can explain *why* a settlement's legitimacy is what it is
    /// (TG-OBS-002 Principle 9, Explainability) rather than showing an
    /// opaque number - see SettlementsController.GetById.
    /// </summary>
    public LegitimacyBreakdown GetLegitimacyBreakdown(Settlement settlement, long tick)
    {
        var leader = settlement.LeaderId != null
            ? _worldState.Citizens.FirstOrDefault(c => c.Id == settlement.LeaderId)
            : null;

        var competence = leader != null ? Math.Min(100, leader.ContributionScore) : 30.0;
        var publicTrust = leader?.Reputation ?? 50.0;
        var ticksSinceChange = tick - settlement.LastGovernmentChangeTick;
        var stability = Math.Clamp(ticksSinceChange / 500.0 * 100.0, 0.0, 100.0);
        var total = Math.Clamp(competence * 0.4 + publicTrust * 0.3 + stability * 0.3, 0.0, 100.0);

        return new LegitimacyBreakdown(competence, publicTrust, stability, total);
    }

    private double CalculateLegitimacy(Settlement settlement, long tick) =>
        GetLegitimacyBreakdown(settlement, tick).Total;
}

public readonly record struct LegitimacyBreakdown(
    double Competence, double PublicTrust, double Stability, double Total);
