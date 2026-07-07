using Microsoft.AspNetCore.SignalR;

namespace Garden.Api.Hubs;

public class SimulationHub : Hub
{
    public async Task SubscribeToSimulation()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "simulation");
    }

    public async Task UnsubscribeFromSimulation()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "simulation");
    }
}

public class EnvironmentHub : Hub
{
    public async Task SubscribeToEnvironment()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "environment");
    }

    public async Task UnsubscribeFromEnvironment()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "environment");
    }
}

public class CitizenHub : Hub
{
    public async Task SubscribeToCitizens()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "citizens");
    }

    public async Task UnsubscribeFromCitizens()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "citizens");
    }
}

public class SettlementHub : Hub
{
    public async Task SubscribeToSettlements()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "settlements");
    }

    public async Task UnsubscribeFromSettlements()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "settlements");
    }
}

public class HistoryHub : Hub
{
    public async Task SubscribeToHistory()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "history");
    }

    public async Task UnsubscribeFromHistory()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "history");
    }
}
