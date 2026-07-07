using Garden.World.Entities;

namespace Garden.Engine.Services;

public class StoryEngine
{
    private readonly HistoricalArchive _archive;
    private readonly List<Story> _stories = [];
    private readonly object _lock = new();

    public IReadOnlyList<Story> Stories
    {
        get { lock (_lock) return _stories.ToList(); }
    }

    public StoryEngine(HistoricalArchive archive)
    {
        _archive = archive;
    }

    public Story GenerateStory(HistoricalRecord record)
    {
        var narrative = GenerateNarrative(record);
        var story = new Story
        {
            Tick = record.Tick,
            Category = record.Category,
            Title = record.Title,
            Summary = narrative.Length > 120 ? narrative[..117] + "..." : narrative,
            Narrative = narrative,
            ParticipantIds = record.ParticipantIds,
            ParticipantNames = record.ParticipantNames,
            RelatedRecordIds = [record.Id.Value.ToString()],
            GeneratedAtTick = _archive.Records.Count > 0 ? _archive.Records.Max(r => r.Tick) : record.Tick
        };

        lock (_lock)
        {
            _stories.Add(story);
        }

        return story;
    }

    public List<Story> GenerateStoriesForCategory(string category, int count = 5)
    {
        var records = _archive.Search(category: category, take: count);
        var stories = new List<Story>();

        foreach (var record in records)
        {
            var existing = _stories.Any(s =>
                s.RelatedRecordIds.Contains(record.Id.Value.ToString()));
            if (!existing)
            {
                stories.Add(GenerateStory(record));
            }
        }

        return stories;
    }

    public Story? GetById(Guid id)
    {
        lock (_lock)
        {
            return _stories.FirstOrDefault(s => s.Id.Value == id);
        }
    }

    private static string GenerateNarrative(HistoricalRecord record)
    {
        var participantStr = record.ParticipantNames.Count > 0
            ? string.Join(", ", record.ParticipantNames)
            : "Unknown";

        var locationStr = !string.IsNullOrEmpty(record.LocationName)
            ? $" at {record.LocationName}"
            : record.LocationX != 0 || record.LocationY != 0
                ? $" near ({record.LocationX}, {record.LocationY})"
                : "";

        var timeStr = $"Year {record.Year}, {record.Season}";

        return record.Category switch
        {
            HistoryCategories.Birth => $"{participantStr} was born on {timeStr}{locationStr}. The world gained a new soul.",
            HistoryCategories.Death => $"{participantStr} passed away on {timeStr}{locationStr}. Their journey has ended.",
            HistoryCategories.Settlement => $"{record.Title} was established on {timeStr}{locationStr}. {participantStr} laid the foundation of a new community.",
            HistoryCategories.Building => $"{record.Description} on {timeStr}{locationStr}. A new structure rises in {participantStr}.",
            HistoryCategories.Disaster => $"{timeStr}{locationStr}: {record.Description}. The world bears the scars of this event.",
            HistoryCategories.Harvest => $"In {timeStr}{locationStr}, {record.Description}. The land provided for the people.",
            HistoryCategories.Discovery => $"{participantStr} discovered something remarkable on {timeStr}{locationStr}. {record.Description}",
            HistoryCategories.Migration => $"{participantStr} migrated on {timeStr}{locationStr}. {record.Description}",
            HistoryCategories.Trade => $"{timeStr}{locationStr}, {record.Description}. Commerce flourished between communities.",
            _ => $"{timeStr}{locationStr}: {record.Description}"
        };
    }
}
