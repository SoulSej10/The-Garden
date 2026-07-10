using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-002 (specification/RFC/RFC-002-communication-knowledge-diffusion.md):
/// first increment of TG-500_Communication.md - whether a citizen has heard
/// about a real civilization milestone event, propagated through the
/// existing Relationship graph (Week 3). No message content, no fidelity
/// loss - just a boolean "have they heard" per (citizen, event) pair.
///
/// KnownEventIds stores a synthetic domain-event key (e.g. "Technology:{id}"),
/// not a HistoricalRecord.Id - HistorySystem never exposes the archive
/// record it generates back to other subscribers of the same domain event,
/// and matching by search after the fact would be fragile (subscription
/// order between HistorySystem and this system is not guaranteed). The
/// synthetic key plus a locally-tracked title (EventTitles) gives the
/// Observatory everything it needs without that coupling.
///
/// Runs daily (IntervalTicks = 24), same cadence as HistorySystem and
/// RelationshipSystem, since nothing about rumor spread needs hourly
/// precision.
/// </summary>
public class CommunicationSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private long _nextExecutionTick;

    public string Name => "CommunicationSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-002: invented thresholds (TG-500 gives no numbers). Loosely
    // mirrors DiplomacyService's band shape without importing it directly -
    // that's settlement-level, this is citizen-level (see RFC-002 open
    // question 2).
    private const double SpreadSocialDistanceThreshold = 40.0;
    private const double SpreadTrustThreshold = 30.0;

    private readonly Dictionary<string, string> _eventTitles = new();

    /// <summary>Synthetic event key -> human-readable title, for Observatory surfacing.</summary>
    public IReadOnlyDictionary<string, string> EventTitles => _eventTitles;

    public CommunicationSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;

        eventBus.Subscribe<TechnologyDiscoveredEvent>(OnTechnologyDiscovered);
        eventBus.Subscribe<KingdomFoundedEvent>(OnKingdomFounded);
        eventBus.Subscribe<ReligionEstablishedEvent>(OnReligionEstablished);
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var citizensById = _worldState.Citizens
            .Where(c => c.IsAlive)
            .ToDictionary(c => c.Id);

        foreach (var rel in _worldState.Relationships)
        {
            if (rel.SocialDistance >= SpreadSocialDistanceThreshold) continue;
            if (!citizensById.TryGetValue(rel.EntityAId, out var a)) continue;
            if (!citizensById.TryGetValue(rel.EntityBId, out var b)) continue;

            var trustAtoB = Math.Min(rel.Trust, b.Emotions.Trust);
            var trustBtoA = Math.Min(rel.Trust, a.Emotions.Trust);
            var spreadChance = (100.0 - rel.SocialDistance) / 100.0;

            if (trustBtoA > SpreadTrustThreshold && a.KnownEventIds.Count > 0 &&
                System.Random.Shared.NextDouble() < spreadChance)
            {
                Diffuse(from: a, to: b);
            }

            if (trustAtoB > SpreadTrustThreshold && b.KnownEventIds.Count > 0 &&
                System.Random.Shared.NextDouble() < spreadChance)
            {
                Diffuse(from: b, to: a);
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    /// <summary>
    /// A single successful "conversation" transfers every event the spreader
    /// knows that the listener doesn't yet - simpler than rolling a
    /// per-event chance, and still produces an organic, relationship-graph-
    /// shaped diffusion curve since the roll to reach this point already
    /// depends on SocialDistance/Trust.
    /// </summary>
    private static void Diffuse(Citizen from, Citizen to)
    {
        foreach (var key in from.KnownEventIds)
        {
            if (!to.KnownEventIds.Contains(key))
                to.KnownEventIds.Add(key);
        }
    }

    private void OnTechnologyDiscovered(TechnologyDiscoveredEvent e)
    {
        var key = $"Technology:{e.TechnologyId}";
        _eventTitles[key] = $"{e.TechnologyName} Is Discovered";
        MarkKnown(e.DiscoveredByCitizenId, key);
    }

    private void OnKingdomFounded(KingdomFoundedEvent e)
    {
        var key = $"Kingdom:{e.KingdomId.Value}";
        _eventTitles[key] = $"The Kingdom of {e.KingdomName} Is Founded";
        MarkKnown(e.LeaderId, key);
    }

    private void OnReligionEstablished(ReligionEstablishedEvent e)
    {
        var key = $"Religion:{e.ReligionId}";
        _eventTitles[key] = $"{e.ReligionName} Is Established";
        MarkKnown(e.FounderCitizenId, key);
    }

    private void MarkKnown(GameEntityId? citizenId, string key)
    {
        if (citizenId is not { } id) return;
        var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id == id && c.IsAlive);
        if (citizen == null) return;
        if (!citizen.KnownEventIds.Contains(key))
            citizen.KnownEventIds.Add(key);
    }
}
