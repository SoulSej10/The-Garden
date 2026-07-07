using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Services;

public class MemoryService
{
    private readonly WorldState _worldState;
    private readonly HistoricalArchive _archive;

    private readonly List<CitizenMemoryRecord> _citizenMemories = [];
    private readonly List<FamilyMemoryRecord> _familyMemories = [];
    private readonly List<CollectiveMemoryRecord> _collectiveMemories = [];

    private readonly object _lock = new();

    public MemoryService(WorldState worldState, HistoricalArchive archive)
    {
        _worldState = worldState;
        _archive = archive;
    }

    public void AddCitizenMemory(string citizenId, long tick, string eventType, string title, string description, double emotionalImpact = 0.5)
    {
        lock (_lock)
        {
            _citizenMemories.Add(new CitizenMemoryRecord
            {
                CitizenId = citizenId,
                Tick = tick,
                EventType = eventType,
                Title = title,
                Description = description,
                EmotionalImpact = emotionalImpact,
                Confidence = 1.0
            });
        }
    }

    public void AddFamilyMemory(string familyId, long tick, string eventType, string title, string description, List<string> ancestorIds)
    {
        lock (_lock)
        {
            _familyMemories.Add(new FamilyMemoryRecord
            {
                FamilyId = familyId,
                Tick = tick,
                EventType = eventType,
                Title = title,
                Description = description,
                AncestorIds = ancestorIds
            });
        }
    }

    public void AddCollectiveMemory(string settlementId, string settlementName, long tick, string eventType, string title, string description, double importance = 1.0)
    {
        lock (_lock)
        {
            _collectiveMemories.Add(new CollectiveMemoryRecord
            {
                SettlementId = settlementId,
                SettlementName = settlementName,
                Tick = tick,
                EventType = eventType,
                Title = title,
                Description = description,
                Importance = importance
            });
        }
    }

    public List<CitizenMemoryRecord> GetCitizenMemories(string citizenId)
    {
        lock (_lock)
        {
            return _citizenMemories
                .Where(m => m.CitizenId == citizenId)
                .OrderByDescending(m => m.Tick)
                .ToList();
        }
    }

    public List<CitizenMemoryRecord> GetAllCitizenMemories(int skip = 0, int take = 50)
    {
        lock (_lock)
        {
            return _citizenMemories
                .OrderByDescending(m => m.Tick)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
    }

    public List<FamilyMemoryRecord> GetFamilyMemories(string familyId)
    {
        lock (_lock)
        {
            return _familyMemories
                .Where(m => m.FamilyId == familyId)
                .OrderByDescending(m => m.Tick)
                .ToList();
        }
    }

    public List<CollectiveMemoryRecord> GetCollectiveMemories(string settlementId)
    {
        lock (_lock)
        {
            return _collectiveMemories
                .Where(m => m.SettlementId == settlementId)
                .OrderByDescending(m => m.Tick)
                .ToList();
        }
    }

    public Dictionary<string, int> GetMemoryStats()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>
            {
                ["citizenMemories"] = _citizenMemories.Count,
                ["familyMemories"] = _familyMemories.Count,
                ["collectiveMemories"] = _collectiveMemories.Count
            };
        }
    }
}
