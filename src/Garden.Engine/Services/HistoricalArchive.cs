using Garden.World.Entities;

namespace Garden.Engine.Services;

public class HistoricalArchive
{
    private readonly List<HistoricalRecord> _records = [];
    private readonly object _lock = new();

    public IReadOnlyList<HistoricalRecord> Records
    {
        get { lock (_lock) return _records.ToList(); }
    }

    public int Count
    {
        get { lock (_lock) return _records.Count; }
    }

    public void Append(HistoricalRecord record)
    {
        lock (_lock)
        {
            _records.Add(record);
        }
    }

    public void AppendRange(IEnumerable<HistoricalRecord> records)
    {
        lock (_lock)
        {
            _records.AddRange(records);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
        }
    }

    public HistoricalRecord? GetById(Guid id)
    {
        lock (_lock)
        {
            return _records.FirstOrDefault(r => r.Id.Value == id);
        }
    }

    public List<HistoricalRecord> Search(
        string? eventType = null,
        string? category = null,
        int? fromTick = null,
        int? toTick = null,
        string? keyword = null,
        string? participantId = null,
        int skip = 0,
        int take = 50)
    {
        lock (_lock)
        {
            var query = _records.AsEnumerable();

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(r => r.EventType.Contains(eventType, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(r => r.Category == category);
            if (fromTick.HasValue)
                query = query.Where(r => r.Tick >= fromTick.Value);
            if (toTick.HasValue)
                query = query.Where(r => r.Tick <= toTick.Value);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(r =>
                    r.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(participantId))
                query = query.Where(r => r.ParticipantIds.Contains(participantId));

            return query
                .OrderByDescending(r => r.Tick)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
    }

    public List<HistoricalRecord> GetTimeline(int? fromTick = null, int? toTick = null, int skip = 0, int take = 50)
    {
        lock (_lock)
        {
            var query = _records.AsEnumerable();

            if (fromTick.HasValue)
                query = query.Where(r => r.Tick >= fromTick.Value);
            if (toTick.HasValue)
                query = query.Where(r => r.Tick <= toTick.Value);

            return query
                .OrderByDescending(r => r.Tick)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
    }

    public Dictionary<string, int> GetEventTypeCounts()
    {
        lock (_lock)
        {
            return _records
                .GroupBy(r => r.EventType)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    public (int Births, int Deaths) GetVitalStats()
    {
        lock (_lock)
        {
            var births = _records.Count(r => r.Category == HistoryCategories.Birth || r.EventType == "CitizenBorn");
            var deaths = _records.Count(r => r.Category == HistoryCategories.Death || r.EventType == "CitizenDied");
            return (births, deaths);
        }
    }
}
