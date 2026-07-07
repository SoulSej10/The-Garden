using Garden.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Garden.Api.Services;

public class BroadcastService
{
    private readonly IHubContext<SimulationHub> _simulationHub;
    private readonly IHubContext<EnvironmentHub> _environmentHub;
    private readonly IHubContext<CitizenHub> _citizenHub;
    private readonly IHubContext<SettlementHub> _settlementHub;
    private readonly IHubContext<HistoryHub> _historyHub;

    public BroadcastService(
        IHubContext<SimulationHub> simulationHub,
        IHubContext<EnvironmentHub> environmentHub,
        IHubContext<CitizenHub> citizenHub,
        IHubContext<SettlementHub> settlementHub,
        IHubContext<HistoryHub> historyHub)
    {
        _simulationHub = simulationHub;
        _environmentHub = environmentHub;
        _citizenHub = citizenHub;
        _settlementHub = settlementHub;
        _historyHub = historyHub;
    }

    public async Task SimulationTick(long tick, bool isPaused, int speed)
    {
        await _simulationHub.Clients.Group("simulation").SendAsync("Tick", new { tick, isPaused, speed });
    }

    public async Task SimulationStatusChanged(string status)
    {
        await _simulationHub.Clients.Group("simulation").SendAsync("StatusChanged", status);
    }

    public async Task PopulationChanged(int total, int alive, int births, int deaths)
    {
        await _citizenHub.Clients.Group("citizens").SendAsync("PopulationChanged", new { total, alive, births, deaths });
    }

    public async Task CitizenActivityChanged(string citizenId, string activity, int tileX, int tileY)
    {
        await _citizenHub.Clients.Group("citizens").SendAsync("CitizenActivityChanged", new { citizenId, activity, tileX, tileY });
    }

    public async Task WeatherUpdated(object weather)
    {
        await _environmentHub.Clients.Group("environment").SendAsync("WeatherUpdated", weather);
    }

    public async Task SeasonChanged(string season)
    {
        await _environmentHub.Clients.Group("environment").SendAsync("SeasonChanged", season);
    }

    public async Task SettlementUpdated(string settlementId, string name, int population, int buildings)
    {
        await _settlementHub.Clients.Group("settlements").SendAsync("SettlementUpdated", new { settlementId, name, population, buildings });
    }

    public async Task SettlementFounded(string name, int tileX, int tileY)
    {
        await _settlementHub.Clients.Group("settlements").SendAsync("SettlementFounded", new { name, tileX, tileY });
    }

    public async Task BuildingCompleted(string settlementName, string buildingType)
    {
        await _settlementHub.Clients.Group("settlements").SendAsync("BuildingCompleted", new { settlementName, buildingType });
    }

    public async Task SignificantEvent(string title, string description, string category, string severity)
    {
        await _historyHub.Clients.Group("history").SendAsync("SignificantEvent", new { title, description, category, severity });
    }

    public async Task StoryGenerated(string title, string summary)
    {
        await _historyHub.Clients.Group("history").SendAsync("StoryGenerated", new { title, summary });
    }

    public async Task CitizenBorn(string name)
    {
        await _citizenHub.Clients.Group("citizens").SendAsync("CitizenBorn", new { name });
        await _historyHub.Clients.Group("history").SendAsync("SignificantEvent", new
        {
            title = $"{name} was born",
            description = $"A new citizen, {name}, has arrived in the world.",
            category = "Birth",
            severity = "Normal"
        });
    }

    public async Task CitizenDied(string name, int age, string cause)
    {
        await _citizenHub.Clients.Group("citizens").SendAsync("CitizenDied", new { name, age, cause });
        await _historyHub.Clients.Group("history").SendAsync("SignificantEvent", new
        {
            title = $"{name} has passed",
            description = $"{name} died at age {age} from {cause}.",
            category = "Death",
            severity = "High"
        });
    }
}
