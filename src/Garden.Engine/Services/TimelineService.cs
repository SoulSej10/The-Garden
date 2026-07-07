using Garden.World.Entities;

namespace Garden.Engine.Services;

public class TimelineService
{
    private readonly HistoricalArchive _archive;

    public TimelineService(HistoricalArchive archive)
    {
        _archive = archive;
    }

    public TimelineResult GetTimeline(
        int? fromTick = null,
        int? toTick = null,
        string? category = null,
        string? eventType = null,
        int page = 1,
        int pageSize = 50)
    {
        var skip = (page - 1) * pageSize;

        var allRecords = _archive.Search(
            eventType: eventType,
            category: category,
            fromTick: fromTick,
            toTick: toTick,
            skip: skip,
            take: pageSize
        );

        var totalMatches = _archive.Search(
            eventType: eventType,
            category: category,
            fromTick: fromTick,
            toTick: toTick
        ).Count;

        return new TimelineResult
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalMatches,
            TotalPages = (int)Math.Ceiling(totalMatches / (double)pageSize),
            Entries = allRecords.Select(r => new TimelineEntry
            {
                Id = r.Id.Value,
                Tick = r.Tick,
                Year = r.Year,
                Day = r.Day,
                Season = r.Season,
                EventType = r.EventType,
                Category = r.Category,
                Title = r.Title,
                Description = r.Description,
                LocationX = r.LocationX,
                LocationY = r.LocationY,
                LocationName = r.LocationName,
                ParticipantNames = r.ParticipantNames,
                Severity = r.Severity,
                Importance = r.Importance
            }).ToList()
        };
    }
}

public class TimelineResult
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages { get; init; }
    public List<TimelineEntry> Entries { get; init; } = [];
}

public class TimelineEntry
{
    public Guid Id { get; init; }
    public long Tick { get; init; }
    public int Year { get; init; }
    public int Day { get; init; }
    public string Season { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int LocationX { get; init; }
    public int LocationY { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public List<string> ParticipantNames { get; init; } = [];
    public double Severity { get; init; }
    public string Importance { get; init; } = "Normal";
}
