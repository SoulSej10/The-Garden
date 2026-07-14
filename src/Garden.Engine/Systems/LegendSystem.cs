using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-016 (specification/RFC/RFC-016-legends-myth-formation.md): first
/// increment of TG-STRY-040_Legends_Myths.md - "Memory changes faster than
/// history. Facts remain constant. Stories evolve." A HistoricalRecord that
/// is already High-importance (SignificanceEvaluator), once old enough
/// (Historical Distance), grows a distorted Legend alongside it - the
/// original record is never overwritten, per TG-STRY-040's "Legends never
/// overwrite objective history. They exist alongside it."
///
/// Yearly cadence (IntervalTicks = SimulationTime.TicksPerYear), matching
/// every other civilization-scale system's established cadence.
/// </summary>
public class LegendSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly HistoricalArchive _archive;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "LegendSystem";
    public long IntervalTicks => SimulationTime.TicksPerYear;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-016: invented thresholds/rates (TG-STRY-040 gives no numbers).
    private const long HistoricalDistanceYears = 3;
    private const double LegendaryStatusGrowthPerYear = 4.0;

    private readonly HashSet<Guid> _legendSourceIds = [];

    public LegendSystem(WorldState worldState, HistoricalArchive archive, IEventBus eventBus)
    {
        _worldState = worldState;
        _archive = archive;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var distanceThreshold = HistoricalDistanceYears * SimulationTime.TicksPerYear;

        foreach (var legend in _worldState.Legends)
            legend.LegendaryStatus = Math.Min(100.0, legend.LegendaryStatus + LegendaryStatusGrowthPerYear);

        var eligible = _archive.Records
            .Where(r => r.Importance == "High")
            .Where(r => tick - r.Tick >= distanceThreshold)
            .Where(r => !_legendSourceIds.Contains(r.Id.Value));

        foreach (var record in eligible)
        {
            var legend = new Legend
            {
                SourceRecordId = record.Id,
                Title = $"The Legend of {record.Title}",
                DistortedNarrative = GenerateDistortion(record),
                FormedTick = tick
            };

            _worldState.Legends.Add(legend);
            _legendSourceIds.Add(record.Id.Value);

            _eventBus.Publish(new LegendFormedEvent
            {
                Tick = tick,
                LegendId = legend.Id,
                SourceRecordId = record.Id,
                Title = legend.Title
            });
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    // RFC-016: category-keyed distortion templates - invented (TG-STRY-040
    // gives no formula), directly implementing the spec's own named
    // transformations (Explorer -> World Founder, General -> Invincible
    // Warrior, a devastating flood -> divine judgment, etc.) as a fixed,
    // honest template set rather than a generative narrative engine.
    private static string GenerateDistortion(HistoricalRecord record)
    {
        var who = record.ParticipantNames.Count > 0
            ? string.Join(" and ", record.ParticipantNames)
            : "a figure whose true name has faded";

        return record.Category switch
        {
            HistoryCategories.Death => $"They say {who} did not truly die, but passed into legend - some claim they achieved the impossible before the end.",
            HistoryCategories.Disaster => $"Elders now tell of {record.Title} as the will of unseen forces, a judgment upon the land rather than mere misfortune.",
            HistoryCategories.Discovery => $"Some say {who} did not discover this at all - it was whispered to them by the world itself, in a dream or a vision.",
            HistoryCategories.War => $"The tale has grown in the telling: {who} is now remembered as unstoppable, a warrior no blade could touch.",
            HistoryCategories.Settlement => $"Children are told {record.Title} was founded on a site chosen by fate, not by the ordinary people who actually chose it.",
            HistoryCategories.Building => "Long after its builders were forgotten, people came to believe it was raised overnight by hands no one saw.",
            _ => $"Over the years, the truth of {record.Title} has blurred - what really happened and what people now believe no longer fully agree."
        };
    }
}
