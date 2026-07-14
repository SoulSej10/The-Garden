using Garden.Core.Time;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

/// <summary>
/// RFC-017 (specification/RFC/RFC-017-save-load-timeline-branching.md):
/// fixes two real defects found while scoping Replay & Timeline Branching -
/// LoadAsync previously left WorldState.CurrentTime and every civilization-
/// level collection (Kingdoms, TradeRoutes, Wars, etc.) untouched, silently
/// contradicting TG-OBS-007's "the restored world should behave exactly as
/// it did." Also adds save lineage (Id/ParentSaveId) so the Observatory can
/// show which save each world continued from, per TG-OBS-007's Timeline
/// Branching section.
/// </summary>
public class SaveLoadService
{
    private readonly WorldState _worldState;
    private readonly HistoricalArchive _archive;
    private readonly SimulationCoordinator _coordinator;
    private readonly ILogger<SaveLoadService> _logger;
    private readonly string _saveDirectory;

    // RFC-017: the save most recently loaded in this process, if any - the
    // next save made becomes its child, capturing exactly the "load an
    // earlier save, then continue" branch point TG-OBS-007's own example
    // describes. Resets to null on process start/world reset, matching
    // this RFC's first open question's recommendation.
    private Guid? _currentParentSaveId;

    public SaveLoadService(
        WorldState worldState,
        HistoricalArchive archive,
        SimulationCoordinator coordinator,
        ILogger<SaveLoadService> logger)
    {
        _worldState = worldState;
        _archive = archive;
        _coordinator = coordinator;
        _logger = logger;
        _saveDirectory = Path.Combine(AppContext.BaseDirectory, "saves");
        Directory.CreateDirectory(_saveDirectory);
    }

    // RFC-017: called by SystemController.ResetWorld so a fresh world
    // starts a new, parentless lineage rather than inheriting whatever
    // save was loaded before the reset.
    public void ResetLineage() => _currentParentSaveId = null;

    public IReadOnlyList<SaveSlot> ListSaves()
    {
        if (!Directory.Exists(_saveDirectory)) return [];
        return Directory.GetFiles(_saveDirectory, "*.save.json")
            .Select(f =>
            {
                try
                {
                    var info = new FileInfo(f);
                    return new SaveSlot
                    {
                        Name = Path.GetFileNameWithoutExtension(f).Replace(".save", ""),
                        FilePath = f,
                        SizeBytes = info.Length,
                        LastModified = info.LastWriteTimeUtc
                    };
                }
                catch { return null; }
            })
            .Where(s => s != null)
            .OrderByDescending(s => s!.LastModified)
            .ToList()!;
    }

    public async Task<bool> SaveAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var snapshot = new WorldSnapshot
            {
                Id = Guid.NewGuid(),
                ParentSaveId = _currentParentSaveId,
                Name = name,
                SavedAt = DateTime.UtcNow,
                Tick = _worldState.CurrentTime.Tick,
                IsRunning = _coordinator.IsRunning,
                Speed = _coordinator.TargetSpeed,
                Citizens = _worldState.Citizens.ToList(),
                Settlements = _worldState.Settlements.ToList(),
                HistoryRecords = _archive.Records.ToList(),
                Kingdoms = _worldState.Kingdoms.ToList(),
                DiplomaticRelations = _worldState.DiplomaticRelations.ToList(),
                TradeRoutes = _worldState.TradeRoutes.ToList(),
                LanguageDivergences = _worldState.LanguageDivergences.ToList(),
                Apprenticeships = _worldState.Apprenticeships.ToList(),
                LegalCases = _worldState.LegalCases.ToList(),
                Infections = _worldState.Infections.ToList(),
                Wars = _worldState.Wars.ToList(),
                SettlementTechnologies = _worldState.SettlementTechnologies.ToList(),
                Legends = _worldState.Legends.ToList(),
                Religions = _worldState.Religions.ToList()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(snapshot, _jsonOptions);
            var path = Path.Combine(_saveDirectory, $"{Sanitize(name)}.save.json");
            await File.WriteAllTextAsync(path, json, ct);

            _logger.LogInformation("World saved as '{Name}' ({Size} bytes)", name, json.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save world as '{Name}'", name);
            return false;
        }
    }

    public async Task<bool> LoadAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var path = Path.Combine(_saveDirectory, $"{Sanitize(name)}.save.json");
            if (!File.Exists(path)) return false;

            var json = await File.ReadAllTextAsync(path, ct);
            var snapshot = System.Text.Json.JsonSerializer.Deserialize<WorldSnapshot>(json, _jsonOptions);
            if (snapshot == null) return false;

            _worldState.CurrentTime = SimulationTime.FromTick(snapshot.Tick);

            _worldState.Citizens.Clear();
            _worldState.Citizens.AddRange(snapshot.Citizens);

            _worldState.Settlements.Clear();
            _worldState.Settlements.AddRange(snapshot.Settlements);

            _worldState.Kingdoms.Clear();
            _worldState.Kingdoms.AddRange(snapshot.Kingdoms);

            _worldState.DiplomaticRelations.Clear();
            _worldState.DiplomaticRelations.AddRange(snapshot.DiplomaticRelations);

            _worldState.TradeRoutes.Clear();
            _worldState.TradeRoutes.AddRange(snapshot.TradeRoutes);

            _worldState.LanguageDivergences.Clear();
            _worldState.LanguageDivergences.AddRange(snapshot.LanguageDivergences);

            _worldState.Apprenticeships.Clear();
            _worldState.Apprenticeships.AddRange(snapshot.Apprenticeships);

            _worldState.LegalCases.Clear();
            _worldState.LegalCases.AddRange(snapshot.LegalCases);

            _worldState.Infections.Clear();
            _worldState.Infections.AddRange(snapshot.Infections);

            _worldState.Wars.Clear();
            _worldState.Wars.AddRange(snapshot.Wars);

            _worldState.SettlementTechnologies.Clear();
            _worldState.SettlementTechnologies.AddRange(snapshot.SettlementTechnologies);

            _worldState.Legends.Clear();
            _worldState.Legends.AddRange(snapshot.Legends);

            _worldState.Religions.Clear();
            _worldState.Religions.AddRange(snapshot.Religions);

            _archive.Clear();
            foreach (var record in snapshot.HistoryRecords)
                _archive.Append(record);

            _currentParentSaveId = snapshot.Id;

            _logger.LogInformation("World loaded from '{Name}' ({Count} citizens, {Settlements} settlements, {History} records)",
                name, snapshot.Citizens.Count, snapshot.Settlements.Count, snapshot.HistoryRecords.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load world '{Name}'", name);
            return false;
        }
    }

    // RFC-017: flat list of every save's lineage pointer - the Observatory
    // builds the branch tree client-side from ParentSaveId, the same
    // "server returns flat data, client renders structure" pattern already
    // used elsewhere (e.g. TerritorySystem.ActiveDisputes).
    public IReadOnlyList<SaveTimelineEntry> GetTimeline()
    {
        var entries = new List<SaveTimelineEntry>();
        if (!Directory.Exists(_saveDirectory)) return entries;

        foreach (var file in Directory.GetFiles(_saveDirectory, "*.save.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var snapshot = System.Text.Json.JsonSerializer.Deserialize<WorldSnapshot>(json, _jsonOptions);
                if (snapshot == null) continue;

                entries.Add(new SaveTimelineEntry(
                    snapshot.Id, snapshot.ParentSaveId, snapshot.Name, snapshot.Tick, snapshot.SavedAt));
            }
            catch { /* skip unreadable/corrupt save files, same as ListSaves */ }
        }

        return entries.OrderBy(e => e.SavedAt).ToList();
    }

    public bool DeleteSave(string name)
    {
        try
        {
            var path = Path.Combine(_saveDirectory, $"{Sanitize(name)}.save.json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            _logger.LogInformation("Deleted save '{Name}'", name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete save '{Name}'", name);
            return false;
        }
    }

    private static string Sanitize(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
        IncludeFields = true
    };
}

public class SaveSlot
{
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime LastModified { get; init; }
}

public class WorldSnapshot
{
    // RFC-017: lineage - Id identifies this save uniquely, ParentSaveId (if
    // any) is the save that was loaded immediately before this one was
    // created, capturing TG-OBS-007's Timeline Branching example.
    public Guid Id { get; init; }
    public Guid? ParentSaveId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime SavedAt { get; init; }
    public long Tick { get; init; }
    public bool IsRunning { get; init; }
    public double Speed { get; init; }
    public List<Citizen> Citizens { get; init; } = [];
    public List<Settlement> Settlements { get; init; } = [];
    public List<HistoricalRecord> HistoryRecords { get; init; } = [];
    // RFC-017: previously missing entirely from the snapshot - LoadAsync
    // left these at whatever the live world state was, contradicting
    // TG-OBS-007's "restored world should behave exactly as it did."
    public List<Kingdom> Kingdoms { get; init; } = [];
    public List<DiplomaticRelation> DiplomaticRelations { get; init; } = [];
    public List<TradeRoute> TradeRoutes { get; init; } = [];
    public List<LanguageDivergence> LanguageDivergences { get; init; } = [];
    public List<Apprenticeship> Apprenticeships { get; init; } = [];
    public List<LegalCase> LegalCases { get; init; } = [];
    public List<Infection> Infections { get; init; } = [];
    public List<War> Wars { get; init; } = [];
    public List<SettlementTechnology> SettlementTechnologies { get; init; } = [];
    public List<Legend> Legends { get; init; } = [];
    public List<Religion> Religions { get; init; } = [];
}

public record SaveTimelineEntry(Guid Id, Guid? ParentSaveId, string Name, long Tick, DateTime SavedAt);
