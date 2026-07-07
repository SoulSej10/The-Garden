using Garden.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Garden.Infrastructure.Services;

public class BackupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupService> _logger;
    private readonly string _backupDirectory;
    private readonly TimeSpan _hourlyInterval = TimeSpan.FromHours(1);

    public BackupService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _backupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup service started. Backups stored in: {Dir}", _backupDirectory);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_hourlyInterval, stoppingToken);

                var hour = DateTime.UtcNow.Hour;
                if (hour == 3)
                    await CreateBackup("daily", stoppingToken);
                else if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday && hour == 3)
                    await CreateBackup("weekly", stoppingToken);
                else
                    await CreateBackup("hourly", stoppingToken);

                PruneOldBackups();
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task CreateBackup(string type, CancellationToken ct)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupName = $"{type}_{timestamp}";
            var backupPath = Path.Combine(_backupDirectory, backupName);
            Directory.CreateDirectory(backupPath);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

            var citizens = await db.Citizens.AsNoTracking().ToListAsync(ct);
            var settlements = await db.Settlements.AsNoTracking().ToListAsync(ct);

            var manifest = new BackupManifest
            {
                Name = backupName,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                CitizenCount = citizens.Count,
                SettlementCount = settlements.Count
            };

            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, _jsonOptions);
            await File.WriteAllTextAsync(Path.Combine(backupPath, "manifest.json"), manifestJson, ct);

            var citizensJson = System.Text.Json.JsonSerializer.Serialize(citizens, _jsonOptions);
            await File.WriteAllTextAsync(Path.Combine(backupPath, "citizens.json"), citizensJson, ct);

            var settlementsJson = System.Text.Json.JsonSerializer.Serialize(settlements, _jsonOptions);
            await File.WriteAllTextAsync(Path.Combine(backupPath, "settlements.json"), settlementsJson, ct);

            _logger.LogInformation("Created {Type} backup '{Name}' ({CitizenCount} citizens, {SettlementCount} settlements)",
                type, backupName, citizens.Count, settlements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {Type} backup", type);
        }
    }

    private void PruneOldBackups()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory)) return;

            var allBackups = Directory.GetDirectories(_backupDirectory)
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.CreationTimeUtc)
                .ToList();

            var hourly = allBackups.Where(d => d.Name.StartsWith("hourly_")).Skip(24).ToList();
            var daily = allBackups.Where(d => d.Name.StartsWith("daily_")).Skip(7).ToList();
            var weekly = allBackups.Where(d => d.Name.StartsWith("weekly_")).Skip(4).ToList();

            foreach (var dir in hourly.Concat(daily).Concat(weekly))
            {
                dir.Delete(recursive: true);
                _logger.LogDebug("Pruned old backup: {Name}", dir.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prune old backups");
        }
    }

    public IReadOnlyList<BackupManifest> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory)) return [];
        return Directory.GetDirectories(_backupDirectory)
            .Select(d =>
            {
                try
                {
                    var manifestPath = Path.Combine(d, "manifest.json");
                    if (!File.Exists(manifestPath)) return null;
                    var json = File.ReadAllText(manifestPath);
                    return System.Text.Json.JsonSerializer.Deserialize<BackupManifest>(json, _jsonOptions);
                }
                catch { return null; }
            })
            .Where(b => b != null)
            .OrderByDescending(b => b!.CreatedAt)
            .ToList()!;
    }

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        IncludeFields = true
    };
}

public class BackupManifest
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int CitizenCount { get; init; }
    public int SettlementCount { get; init; }
}
