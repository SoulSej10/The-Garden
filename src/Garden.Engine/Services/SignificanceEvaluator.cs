using Garden.World.Entities;

namespace Garden.Engine.Services;

public class SignificanceEvaluator
{
    // DEVELOPMENT_PLAN.md Week 4 Day 18: TG-STRY-050's Core Principle is
    // "Events become historical because of their consequences, not their
    // spectacle" - Historical Importance is supposed to weigh Duration,
    // Breadth of influence, Depth of consequence, Number of affected
    // systems, Persistence, and Narrative continuity, never event type
    // alone. "FarmHarvested" used to sit in this hashset, forcing every
    // single harvest - including trivially small ones every few ticks - to
    // be archived as "High" regardless of severity, flooding
    // HistoricalArchive with near-duplicate "Harvest at X" records (directly
    // observed live: dozens of them for one settlement within a few in-game
    // days). Removed so its real per-harvest severity (now scaled by actual
    // Yield in HistorySystem.OnFarmHarvested - "Depth of consequence")
    // decides significance instead of the event type alone. This audit's
    // scope was one concrete fix, not a full causal-chain/duration-tracking
    // rewrite (Duration/Persistence/Narrative continuity still aren't
    // tracked over time anywhere in this codebase - a larger, separate
    // effort). "TradeCompleted" has the identical unconditional-High problem
    // and is left as a known follow-up, not fixed here, since
    // TradeCompletedEvent is never actually published anywhere in this
    // codebase yet (dead code - see SPEC_INDEX.md Week 3 Day 13 finding) and
    // so isn't causing any live archive-flooding today.
    private static readonly HashSet<string> HighImportanceEvents =
    [
        "CitizenBorn", "CitizenDied", "SettlementFounded", "SettlementExpanded",
        "BuildingCompleted", "TradeCompleted",
        "SettlementAbandoned", "CitizenMurdered", "WarDeclared",
        "MajorFlood", "LongDrought", "Earthquake", "VolcanicEruption",
        "Plague", "Wildfire",
        // Civilization milestones (TG-008 significance criteria: founding events,
        // political transitions, technological breakthroughs) - always archived
        // regardless of the severity value a caller passes in.
        "KingdomFounded", "KingdomDissolved", "TechnologyDiscovered", "ReligionEstablished"
    ];

    private static readonly HashSet<string> MediumImportanceEvents =
    [
        "BuildingPlanned", "ResourceGathered", "GoodsCrafted",
        "FarmPlanted", "CitizenAged"
    ];

    public string Evaluate(string eventType, double? severity = null)
    {
        if (HighImportanceEvents.Contains(eventType))
            return "High";

        if (MediumImportanceEvents.Contains(eventType))
            return "Medium";

        if (severity.HasValue && severity.Value > 7.0)
            return "High";
        if (severity.HasValue && severity.Value > 4.0)
            return "Medium";

        return "Low";
    }

    public bool ShouldArchive(string eventType, string importance)
    {
        if (importance == "High" || importance == "Medium")
            return true;

        return false;
    }
}
