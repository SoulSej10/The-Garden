using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-005 (specification/RFC/RFC-005-law-dispute-resolution.md): first
/// increment of TG-590_Law_Justice.md - informal dispute resolution
/// between two citizens in the same settlement, resolved by the
/// settlement's existing leader, gated by its existing Legitimacy score.
/// No formal institutions (courts, judges, juries) exist yet.
///
/// Yearly cadence (IntervalTicks = 336), matching TechnologyService/
/// ReligionService/KingdomService/LanguageSystem/EducationSystem's
/// existing convention in CivilizationSystem.
/// </summary>
public class LawSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "LawSystem";
    public long IntervalTicks => 336;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-005: invented threshold (TG-590 gives no numbers) - "actively
    // hostile," distinct from RFC-002/003/004's SocialDistance-based gate.
    private const double DisputeTrustThreshold = 20.0;
    private const int UnresolvedYearsBeforeFailure = 3;
    private const double TrustRestoredOnResolution = 15.0;

    public LawSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var citizensById = _worldState.Citizens.Where(c => c.IsAlive).ToDictionary(c => c.Id);
        var settlementsById = _worldState.Settlements.ToDictionary(s => s.Id);

        ResolveOpenCases(tick, citizensById, settlementsById);
        DetectNewDisputes(tick, citizensById);

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void DetectNewDisputes(long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        foreach (var rel in _worldState.Relationships)
        {
            if (rel.Trust >= DisputeTrustThreshold) continue;
            if (!citizensById.TryGetValue(rel.EntityAId, out var a)) continue;
            if (!citizensById.TryGetValue(rel.EntityBId, out var b)) continue;
            if (a.HomeSettlementId == null || a.HomeSettlementId != b.HomeSettlementId) continue;

            var alreadyOpen = _worldState.LegalCases.Any(c =>
                c.IsOpen &&
                ((c.CitizenAId == a.Id && c.CitizenBId == b.Id) ||
                 (c.CitizenAId == b.Id && c.CitizenBId == a.Id)));
            if (alreadyOpen) continue;

            _worldState.LegalCases.Add(new LegalCase
            {
                SettlementId = a.HomeSettlementId.Value,
                CitizenAId = a.Id,
                CitizenBId = b.Id,
                OpenedTick = tick
            });
        }
    }

    private void ResolveOpenCases(long tick, Dictionary<GameEntityId, Citizen> citizensById,
        Dictionary<GameEntityId, Settlement> settlementsById)
    {
        foreach (var legalCase in _worldState.LegalCases.Where(c => c.IsOpen))
        {
            if (!settlementsById.TryGetValue(legalCase.SettlementId, out var settlement))
            {
                Close(legalCase, tick, resolvedFairly: false, citizensById, settlement: null);
                continue;
            }

            var resolutionChance = Math.Clamp(settlement.Legitimacy / 100.0, 0.0, 1.0);
            if (System.Random.Shared.NextDouble() < resolutionChance)
            {
                if (citizensById.TryGetValue(legalCase.CitizenAId, out var a) &&
                    citizensById.TryGetValue(legalCase.CitizenBId, out var b))
                {
                    var rel = _worldState.Relationships.FirstOrDefault(r =>
                        (r.EntityAId == a.Id && r.EntityBId == b.Id) ||
                        (r.EntityAId == b.Id && r.EntityBId == a.Id));
                    if (rel != null)
                        rel.Trust = Math.Min(100.0, rel.Trust + TrustRestoredOnResolution);
                }

                Close(legalCase, tick, resolvedFairly: true, citizensById, settlement);
                continue;
            }

            if (tick - legalCase.OpenedTick >= UnresolvedYearsBeforeFailure * IntervalTicks)
            {
                Close(legalCase, tick, resolvedFairly: false, citizensById, settlement);
            }
        }
    }

    private void Close(LegalCase legalCase, long tick, bool resolvedFairly,
        Dictionary<GameEntityId, Citizen> citizensById, Settlement? settlement)
    {
        legalCase.IsOpen = false;
        legalCase.ResolvedTick = tick;
        legalCase.WasResolvedFairly = resolvedFairly;

        var aName = citizensById.TryGetValue(legalCase.CitizenAId, out var a)
            ? $"{a.FirstName} {a.LastName}" : "Unknown";
        var bName = citizensById.TryGetValue(legalCase.CitizenBId, out var b)
            ? $"{b.FirstName} {b.LastName}" : "Unknown";
        var settlementName = settlement?.Name ?? "Unknown";

        if (resolvedFairly)
        {
            _eventBus.Publish(new CaseResolvedEvent
            {
                Tick = tick,
                SettlementId = legalCase.SettlementId,
                SettlementName = settlementName,
                CitizenAId = legalCase.CitizenAId,
                CitizenAName = aName,
                CitizenBId = legalCase.CitizenBId,
                CitizenBName = bName
            });
        }
        else
        {
            _eventBus.Publish(new JusticeFailureEvent
            {
                Tick = tick,
                SettlementId = legalCase.SettlementId,
                SettlementName = settlementName,
                CitizenAId = legalCase.CitizenAId,
                CitizenAName = aName,
                CitizenBId = legalCase.CitizenBId,
                CitizenBName = bName
            });
        }
    }
}
