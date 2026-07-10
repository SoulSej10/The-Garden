using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;
using static Garden.World.Entities.CitizenNeeds;

namespace Garden.Engine.Systems;

/// <summary>
/// First increment of TG-330_Emotion.md, scoped by
/// specification/RFC/RFC-001-emotion-system.md: 6 of the 15 named emotions,
/// each with a real trigger already available in this codebase (Needs,
/// Reputation, Personality, HomeSettlementId, or an existing domain event) -
/// see the RFC for why the other 9 are out of scope for this increment.
/// EmotionSystem is the sole owner of Citizen.Emotions; no other system
/// should mutate it.
/// </summary>
public class EmotionSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private long _nextExecutionTick;

    public string Name => "EmotionSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    // Half-lives in ticks (1 tick = 1 hour, TG-004). Invented - TG-330 gives
    // no numbers, only relative framing ("Startle" fast, "Love" decades).
    // Documented in RFC-001; treat as a one-line change to retune later.
    private const double FearHalfLife = 6;
    private const double JoyHalfLife = 24;
    private const double SadnessHalfLife = 168;
    private const double TrustHalfLife = 720;
    private const double CuriosityHalfLife = 12;
    private const double LonelinessHalfLife = 48;

    public EmotionSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;

        eventBus.Subscribe<SettlementFoundedEvent>(OnSettlementFounded);
        eventBus.Subscribe<CitizenBornEvent>(OnCitizenBorn);
        eventBus.Subscribe<CitizenDiedEvent>(OnCitizenDied);
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var citizen in _worldState.Citizens)
        {
            if (!citizen.IsAlive) continue;
            UpdateEmotions(citizen);
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private static void UpdateEmotions(Citizen citizen)
    {
        var emotions = citizen.Emotions;
        var needs = citizen.Needs;

        var hasCriticalNeed = needs.Hunger >= HungerCriticalThreshold
            || needs.Thirst >= ThirstCriticalThreshold
            || needs.Energy <= EnergyCriticalThreshold
            || needs.Warmth <= WarmthCriticalThreshold
            || needs.Health <= HealthCriticalThreshold;

        // A citizen who was meaningfully afraid and is no longer in danger
        // feels relief - a small, real trigger for Joy (TG-330: "Needs
        // recovering after being critical") using only state already
        // tracked here, no extra bookkeeping fields required.
        var wasAfraid = emotions.Fear > 20.0;

        emotions.Fear = StepToward(emotions.Fear, hasCriticalNeed ? 70.0 : 0.0, FearHalfLife);

        if (wasAfraid && !hasCriticalNeed)
            emotions.Joy = Math.Min(100, emotions.Joy + 10.0);
        emotions.Joy = StepToward(emotions.Joy, 0.0, JoyHalfLife);

        emotions.Sadness = StepToward(emotions.Sadness, 0.0, SadnessHalfLife);

        // Trust settles toward the citizen's own Reputation rather than a
        // fixed neutral point - per RFC-001, this is Trust's real trigger
        // for this increment (pairwise Relationship.Trust from TG-380 is a
        // deliberately separate, later concept - see RFC-001 open question 2).
        emotions.Trust = StepToward(emotions.Trust, citizen.Reputation, TrustHalfLife);

        // Curiosity's transient state settles toward the citizen's stable
        // Personality.Curiosity trait (0-10 scale per SpawnSystem, scaled to
        // this state's 0-100 range) rather than toward zero.
        emotions.Curiosity = StepToward(emotions.Curiosity, citizen.Personality.Curiosity * 10.0, CuriosityHalfLife);

        var lonelinessTarget = citizen.HomeSettlementId == null ? 60.0 : 0.0;
        emotions.Loneliness = StepToward(emotions.Loneliness, lonelinessTarget, LonelinessHalfLife);
    }

    /// <summary>
    /// RFC-001's decay formula: value moves toward target by the fraction
    /// implied by the half-life every tick (ticksElapsed=1 per call, since
    /// this runs every tick rather than tracking a last-update tick per
    /// emotion). "Target" doubles as the emotion's floor/baseline for
    /// continuously-driven emotions (Fear, Trust, Curiosity, Loneliness) and
    /// as a fixed zero for event-driven spikes that should fade (Joy, Sadness).
    /// </summary>
    private static double StepToward(double current, double target, double halfLifeTicks)
    {
        var factor = Math.Pow(0.5, 1.0 / halfLifeTicks);
        return target + (current - target) * factor;
    }

    private void OnSettlementFounded(SettlementFoundedEvent e)
    {
        var founder = _worldState.Citizens.FirstOrDefault(c => c.Id == e.FounderId);
        if (founder != null) founder.Emotions.Joy = Math.Min(100, founder.Emotions.Joy + 40.0);
    }

    private void OnCitizenBorn(CitizenBornEvent e)
    {
        // The newborn has no meaningful emotional history yet - it's the
        // parents who feel joy at a birth.
        var parentA = _worldState.Citizens.FirstOrDefault(c => c.Id == e.ParentAId);
        var parentB = _worldState.Citizens.FirstOrDefault(c => c.Id == e.ParentBId);
        if (parentA != null) parentA.Emotions.Joy = Math.Min(100, parentA.Emotions.Joy + 35.0);
        if (parentB != null) parentB.Emotions.Joy = Math.Min(100, parentB.Emotions.Joy + 35.0);
    }

    private void OnCitizenDied(CitizenDiedEvent e)
    {
        // Grieving is scoped to living settlement-mates - the closest real
        // social-graph proxy available this increment. Explicit family
        // links (parent/child) aren't persisted on Citizen itself (only
        // appear transiently in CitizenBornEvent's args), so true family
        // grief is deferred until that data model exists.
        var deceased = _worldState.Citizens.FirstOrDefault(c => c.Id == e.CitizenId);
        if (deceased?.HomeSettlementId == null) return;

        var settlementMates = _worldState.Citizens.Where(c =>
            c.IsAlive && c.Id != e.CitizenId && c.HomeSettlementId == deceased.HomeSettlementId);

        foreach (var mate in settlementMates)
            mate.Emotions.Sadness = Math.Min(100, mate.Emotions.Sadness + 30.0);
    }
}
