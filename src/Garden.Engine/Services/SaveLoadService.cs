using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class SaveLoadService
{
    private readonly WorldState _worldState;
    private readonly HistoricalArchive _archive;
    private readonly SimulationCoordinator _coordinator;
    private readonly ILogger<SaveLoadService> _logger;
    private readonly string _saveDirectory;

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
                Name = name,
                SavedAt = DateTime.UtcNow,
                Tick = _worldState.CurrentTime.Tick,
                IsRunning = _coordinator.IsRunning,
                Speed = _coordinator.TargetSpeed,
                Citizens = _worldState.Citizens.ToList(),
                Settlements = _worldState.Settlements.ToList(),
                HistoryRecords = _archive.Records.ToList()
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

            _worldState.Citizens.Clear();
            _worldState.Citizens.AddRange(snapshot.Citizens);

            _worldState.Settlements.Clear();
            _worldState.Settlements.AddRange(snapshot.Settlements);

            _archive.Clear();
            foreach (var record in snapshot.HistoryRecords)
                _archive.Append(record);

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
    public string Name { get; init; } = string.Empty;
    public DateTime SavedAt { get; init; }
    public long Tick { get; init; }
    public bool IsRunning { get; init; }
    public double Speed { get; init; }
    public List<Citizen> Citizens { get; init; } = [];
    public List<Settlement> Settlements { get; init; } = [];
    public List<HistoricalRecord> HistoryRecords { get; init; } = [];
}
