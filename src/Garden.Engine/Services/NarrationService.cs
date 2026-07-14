using Garden.Core.Time;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class NarrationService
{
    private readonly WorldState _worldState;
    private readonly HistoricalArchive _archive;
    private readonly ILogger<NarrationService> _logger;

    public NarrationService(
        WorldState worldState,
        HistoricalArchive archive,
        ILogger<NarrationService> logger)
    {
        _worldState = worldState;
        _archive = archive;
        _logger = logger;
    }

    public WorldSummary GenerateSummary()
    {
        var time = _worldState.CurrentTime;
        var alive = _worldState.Citizens.Where(c => c.IsAlive).ToList();
        var settlements = _worldState.Settlements;
        var kingdoms = _worldState.Kingdoms.Where(k => k.IsActive).ToList();
        var records = _archive.Records;
        var recentRecords = records.OrderByDescending(r => r.Tick).Take(20).ToList();

        var narrative = GenerateNarrative(time, alive, settlements, kingdoms, recentRecords);

        return new WorldSummary
        {
            Tick = time.Tick,
            Year = time.Year,
            Day = time.Day,
            Season = time.Season.ToString(),
            Narrative = narrative,
            Statistics = new WorldStats
            {
                TotalCitizens = _worldState.Citizens.Count,
                AliveCitizens = alive.Count,
                DeadCitizens = _worldState.Citizens.Count - alive.Count,
                TotalSettlements = settlements.Count,
                TotalKingdoms = kingdoms.Count,
                TotalBuildings = settlements.Sum(s => s.CompletedBuildings),
                TotalTradeRoutes = _worldState.TradeRoutes.Count(r => r.IsActive),
                TechnologiesDiscovered = _worldState.SettlementTechnologies.Count(t => t.IsDiscovered),
                HistoryRecordCount = records.Count
            },
            Insights = BuildInsights(time, alive, settlements, kingdoms, records)
        };
    }

    /// <summary>
    /// Every insight here is computed directly from live simulation state or
    /// the historical archive - nothing here is invented, per "Answers must
    /// always reference historical records... never fabricate simulation
    /// facts." Sections the simulation doesn't yet model (culture depth,
    /// detailed economic forecasting) are simply omitted rather than
    /// guessed at.
    /// </summary>
    private List<WorldInsight> BuildInsights(
        SimulationTime time, List<Citizen> alive, List<Settlement> settlements,
        List<Kingdom> kingdoms, IReadOnlyList<HistoricalRecord> records)
    {
        var insights = new List<WorldInsight>();

        // Overall world health
        var avgHealth = alive.Count > 0 ? alive.Average(c => c.Needs.Health) : 0;
        insights.Add(new WorldInsight("World Health",
            alive.Count == 0
                ? "No living citizens remain - the world is currently uninhabited."
                : $"{alive.Count} citizens alive, averaging {avgHealth:F0}/100 health. " +
                  (avgHealth >= 70 ? "The population is thriving." : avgHealth >= 40 ? "The population is under moderate stress." : "The population is in poor condition.")));

        // Population trends (recent births vs deaths from history)
        var recentBirths = records.Count(r => r.Category == HistoryCategories.Birth && r.Tick >= time.Tick - 24 * 30);
        var recentDeaths = records.Count(r => r.Category == HistoryCategories.Death && r.Tick >= time.Tick - 24 * 30);
        insights.Add(new WorldInsight("Population Trend",
            $"In the last in-game month: {recentBirths} birth(s), {recentDeaths} death(s). " +
            (recentBirths > recentDeaths ? "Population is growing." : recentBirths < recentDeaths ? "Population is declining." : "Population is holding steady.")));

        // Food security
        var totalFood = settlements.Sum(s => s.Storage.GetQuantity("Food"));
        var foodPerCapita = alive.Count > 0 ? totalFood / alive.Count : 0;
        insights.Add(new WorldInsight("Food Security",
            settlements.Count == 0
                ? "No settlements exist yet, so there is no stored food supply - citizens rely on individual foraging."
                : $"Settlements hold {totalFood:F0} stored food ({foodPerCapita:F1} per living citizen). " +
                  (foodPerCapita >= 5 ? "Reserves look healthy." : foodPerCapita >= 1 ? "Reserves are thin." : "Reserves are critically low.")));

        // Resource availability
        var wood = settlements.Sum(s => s.Storage.GetQuantity("Wood"));
        var stone = settlements.Sum(s => s.Storage.GetQuantity("Stone"));
        var clay = settlements.Sum(s => s.Storage.GetQuantity("Clay"));
        insights.Add(new WorldInsight("Resource Availability",
            $"Settlements hold {wood:F0} wood, {stone:F0} stone, {clay:F0} clay in storage."));

        // Emerging civilizations / settlement growth
        insights.Add(new WorldInsight("Settlements & Civilizations",
            settlements.Count == 0
                ? "No settlements have formed yet."
                : $"{settlements.Count} settlement(s) with {settlements.Sum(s => s.Population)} residents. " +
                  (kingdoms.Count > 0
                      ? $"{kingdoms.Count} kingdom(s) have formed: {string.Join(", ", kingdoms.Select(k => k.Name))}."
                      : "No settlements have united into a kingdom yet.")));

        // Climate conditions
        var weather = _worldState.Weather;
        insights.Add(new WorldInsight("Climate Conditions",
            $"Currently {time.Season}, weather is {weather.CurrentWeather} " +
            $"(intensity {weather.Intensity:F1}, temperature modifier {weather.TemperatureModifier:+0.0;-0.0}°C)."));

        // Notable historical events
        var notable = records.Where(r => r.Importance is "High" or "Critical")
            .OrderByDescending(r => r.Tick).Take(3).ToList();
        insights.Add(new WorldInsight("Notable Recent Events",
            notable.Count > 0
                ? string.Join(" ", notable.Select(r => $"{r.Title} (Year {r.Year})."))
                : "No high-importance events recorded recently."));

        // Potential risks
        var risks = new List<string>();
        if (foodPerCapita < 2 && alive.Count > 0) risks.Add("food shortage");
        if (avgHealth < 50 && alive.Count > 0) risks.Add("widespread poor health");
        if (recentDeaths > recentBirths * 2 && recentDeaths > 0) risks.Add("population decline outpacing births");
        if (settlements.Any(s => !s.HasAvailableHousing)) risks.Add("housing shortages in at least one settlement");
        insights.Add(new WorldInsight("Potential Risks",
            risks.Count > 0 ? $"Watch for: {string.Join(", ", risks)}." : "No significant risks detected from current data."));

        // Exploration / discoveries
        var discoveries = records.Count(r => r.Category == HistoryCategories.Discovery);
        insights.Add(new WorldInsight("Exploration & Discoveries",
            $"{discoveries} discovery/discoveries recorded in history " +
            $"and {_worldState.SettlementTechnologies.Count(t => t.IsDiscovered)} technologies discovered."));

        // Recommendation for investigation
        var recommendation = risks.Count > 0
            ? $"Investigate the {risks[0]} first - it is the most immediate threat to the population."
            : settlements.Count == 0
                ? "Watch whether any citizens successfully found a settlement."
                : "The world is currently stable - no urgent investigation needed.";
        insights.Add(new WorldInsight("Recommendation", recommendation));

        return insights;
    }

    public string AnswerQuestion(string question)
    {
        var q = question.ToLowerInvariant();
        var time = _worldState.CurrentTime;
        var alive = _worldState.Citizens.Where(c => c.IsAlive).ToList();
        var settlements = _worldState.Settlements;
        var kingdoms = _worldState.Kingdoms.Where(k => k.IsActive).ToList();
        var records = _archive.Records;

        if (q.Contains("population") || q.Contains("how many people") || q.Contains("citizens"))
            return $"There are {alive.Count} living citizens out of {_worldState.Citizens.Count} total. The population spans {settlements.Sum(s => s.Population)} settled individuals across {settlements.Count} settlements.";

        if (q.Contains("kingdom") || q.Contains("kingdom collapse") || q.Contains("fall"))
            return kingdoms.Count > 0
                ? $"There are {kingdoms.Count} active kingdoms: {string.Join(", ", kingdoms.Select(k => $"{k.Name} (capital: {k.CapitalName}, ruler: {k.LeaderName})"))}."
                : "No kingdoms have formed yet. Kingdoms emerge when multiple settlements develop stable leadership and form alliances.";

        if (q.Contains("settlement") || q.Contains("town"))
        {
            var largest = settlements.OrderByDescending(s => s.Population).FirstOrDefault();
            return largest != null
                ? $"There are {settlements.Count} settlements. The largest is {largest.Name} with {largest.Population} citizens, led by {largest.LeaderName ?? "no leader"} under {largest.GovernmentType} government."
                : "No settlements have been founded yet.";
        }

        if (q.Contains("oldest") || q.Contains("elder") || q.Contains("senior"))
        {
            var oldest = alive.OrderByDescending(c => c.Age).FirstOrDefault();
            return oldest != null
                ? $"The oldest living citizen is {oldest.FirstName} {oldest.LastName}, aged {oldest.Age} years."
                : "No living citizens.";
        }

        if (q.Contains("year") || q.Contains("time") || q.Contains("age"))
            return $"The world is in year {time.Year}, day {time.Day} of the current year. The season is {time.Season}. Simulation tick: {time.Tick}.";

        if (q.Contains("leader") || q.Contains("chief") || q.Contains("ruler"))
        {
            var leaders = settlements.Where(s => s.LeaderId != null).ToList();
            return leaders.Count > 0
                ? $"There are {leaders.Count} settlement leaders. They include {string.Join(", ", leaders.Take(5).Select(l => $"{l.LeaderName} of {l.Name}"))}{(leaders.Count > 5 ? $" and {leaders.Count - 5} more" : "")}."
                : "No leaders have emerged yet. Leadership develops through community contributions.";
        }

        if (q.Contains("trade"))
        {
            var routes = _worldState.TradeRoutes.Where(r => r.IsActive).ToList();
            return routes.Count > 0
                ? $"There are {routes.Count} active trade routes. The busiest carries {routes.OrderByDescending(r => r.TripCount).First().PrimaryGood} between settlements."
                : "No trade routes are active. Trade develops when settlements produce surpluses and demand exists elsewhere.";
        }

        if (q.Contains("religion") || q.Contains("faith") || q.Contains("belief"))
        {
            var religions = _worldState.Religions.ToList();
            return religions.Count > 0
                ? $"There are {religions.Count} established religions. The largest is {religions.OrderByDescending(r => r.FollowerCount).First().Name} with {religions.Max(r => r.FollowerCount)} followers."
                : "No religions have formed yet. Belief systems emerge from shared values and community traditions.";
        }

        if (q.Contains("tech") || q.Contains("discover") || q.Contains("invent"))
        {
            // RFC-015: discovery is per-settlement now, so a technology name
            // discovered by more than one settlement is de-duplicated here -
            // this narration cares about which technologies exist in the
            // world, not how many settlements independently found each one.
            var discoveredNames = _worldState.SettlementTechnologies
                .Where(t => t.IsDiscovered)
                .Select(t => t.TechnologyName)
                .Distinct()
                .ToList();
            return discoveredNames.Count > 0
                ? $"{discoveredNames.Count} technologies have been discovered. They include {string.Join(", ", discoveredNames)}."
                : "No technologies have been discovered yet. Technology advances as settlements accumulate experience.";
        }

        if (q.Contains("war") || q.Contains("conflict") || q.Contains("hostile"))
        {
            var hostile = _worldState.DiplomaticRelations.Where(r => r.CurrentRelation == RelationType.Hostile).ToList();
            return hostile.Count > 0
                ? $"There are {hostile.Count} hostile relationships between settlements."
                : "There are no hostile relationships currently. Diplomatic tensions may develop as settlements compete for resources.";
        }

        if (q.Contains("death") || q.Contains("die") || q.Contains("mortality"))
        {
            var deaths = _archive.Records.Count(r => r.Category == HistoryCategories.Death);
            var topCause = _archive.Records
                .Where(r => r.Category == HistoryCategories.Death)
                .GroupBy(r => r.Description.Contains("Starvation") ? "Starvation" : r.Description.Contains("Old Age") ? "Old Age" : "Other")
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            return $"There have been {deaths} recorded deaths.{(topCause != null ? $" The most common cause is {topCause.Key} ({topCause.Count()} deaths)." : "")}";
        }

        return "I do not have enough information to answer that question. The historical archive contains records of simulation events, but I cannot fabricate facts that do not exist in the records.";
    }

    private static string GenerateNarrative(
        SimulationTime time, List<Citizen> alive, List<Settlement> settlements,
        List<Kingdom> kingdoms, List<HistoricalRecord> recentRecords)
    {
        var parts = new List<string>();

        parts.Add($"Year {time.Year}, {time.Season}. Day {time.Day}.");

        if (alive.Count > 0)
            parts.Add($"There are {alive.Count} living citizens.");

        if (settlements.Count > 0)
        {
            var totalPop = settlements.Sum(s => s.Population);
            parts.Add($"{settlements.Count} settlements house {totalPop} citizens.");
            var largest = settlements.OrderByDescending(s => s.Population).First()!;
            parts.Add($"The largest is {largest.Name} ({largest.Population} people, led by {largest.LeaderName ?? "no leader"}).");
        }

        if (kingdoms.Count > 0)
            parts.Add($"{kingdoms.Count} active {(kingdoms.Count == 1 ? "kingdom" : "kingdoms")}: {string.Join(", ", kingdoms.Select(k => $"{k.Name} ({k.Population} pop)"))}.");

        var recentEvents = recentRecords.Take(5).ToList();
        if (recentEvents.Count > 0)
        {
            var eventDescriptions = recentEvents.Select(r =>
                $"{r.Title}: {r.Description}").ToList();
            parts.Add("Recent events: " + string.Join(" | ", eventDescriptions));
        }
        else
        {
            parts.Add("No significant recent events.");
        }

        return string.Join(" ", parts);
    }
}

public class WorldSummary
{
    public long Tick { get; init; }
    public int Year { get; init; }
    public int Day { get; init; }
    public string Season { get; init; } = string.Empty;
    public string Narrative { get; init; } = string.Empty;
    public WorldStats Statistics { get; init; } = new();
    public List<WorldInsight> Insights { get; init; } = [];
}

public record WorldInsight(string Topic, string Summary);

public class WorldStats
{
    public int TotalCitizens { get; init; }
    public int AliveCitizens { get; init; }
    public int DeadCitizens { get; init; }
    public int TotalSettlements { get; init; }
    public int TotalKingdoms { get; init; }
    public int TotalBuildings { get; init; }
    public int TotalTradeRoutes { get; init; }
    public int TechnologiesDiscovered { get; init; }
    public int HistoryRecordCount { get; init; }
}
