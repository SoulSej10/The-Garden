using Garden.Core.Events;
using Garden.Core.Time;
using Garden.World.Entities;

namespace Garden.World.Collections;

public class WorldState
{
    public SimulationTime CurrentTime { get; set; }
    public List<Citizen> Citizens { get; } = [];
    public List<Settlement> Settlements { get; } = [];
    public List<Kingdom> Kingdoms { get; } = [];
    public List<DiplomaticRelation> DiplomaticRelations { get; } = [];
    public List<Relationship> Relationships { get; } = [];
    public List<TradeRoute> TradeRoutes { get; } = [];
    public List<Technology> Technologies { get; } = [];
    public List<Religion> Religions { get; } = [];
    public WorldMap Map { get; set; } = new();
    public WeatherStateData Weather { get; set; } = new();
    public List<ClimateData> ClimateZones { get; } = [];
    public List<EnvironmentalEvent> EnvironmentEvents { get; } = [];
    public bool IsInitialized { get; set; }
}
