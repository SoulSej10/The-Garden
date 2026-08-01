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
    public List<LanguageDivergence> LanguageDivergences { get; } = [];
    public List<Apprenticeship> Apprenticeships { get; } = [];
    public List<LegalCase> LegalCases { get; } = [];
    public List<Infection> Infections { get; } = [];
    public List<War> Wars { get; } = [];
    // RFC-015: replaced by SettlementTechnology (ADR-004 - a single shared
    // Technology.CurrentProgress/IsDiscovered per named technology made
    // independent per-settlement discovery structurally impossible).
    // Technology.AllTechnologies (static) remains the read-only catalog.
    public List<SettlementTechnology> SettlementTechnologies { get; } = [];
    public List<Legend> Legends { get; } = [];
    public List<Religion> Religions { get; } = [];
    public WorldMap Map { get; set; } = new();
    // Legacy global aggregate (dominant condition across all weather cells)
    // - kept so existing consumers (EnvironmentController, HydrologySystem)
    // don't need to change; WeatherCells is the new source of truth for
    // spatial rendering. See WeatherSystem for how the two stay in sync.
    public WeatherStateData Weather { get; set; } = new();
    public WeatherCell[,] WeatherCells { get; set; } = new WeatherCell[0, 0];
    public int WeatherCellsX { get; set; }
    public int WeatherCellsY { get; set; }
    public int WeatherCellTileSize { get; set; } = 32;
    public List<ClimateData> ClimateZones { get; } = [];
    public Garden.Core.World.WorldArchetype Archetype { get; set; }
    public List<EnvironmentalEvent> EnvironmentEvents { get; } = [];
    public bool IsInitialized { get; set; }
}
