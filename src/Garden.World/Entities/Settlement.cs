using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// TG-650_Cities_Urbanization.md describes settlement growth on a
// hamlet-to-metropolis hierarchy but gives no numeric thresholds - these are
// invented for this first pass and derived purely from Population, matching
// TG-001's "simplicity first" law. TG-650 also ties the top tiers to
// political role (regional/national capital), which this simplification
// does not yet capture; revisit via an RFC if that distinction is needed.
public enum SettlementTier
{
    Hamlet,
    Village,
    Town,
    City,
    RegionalCapital,
    Metropolis
}

public class Settlement
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int TerritoryRadius { get; set; } = 5;
    public Inventory Storage { get; set; } = new() { MaxCapacity = 500 };
    public List<Building> Buildings { get; init; } = [];
    public List<GameEntityId> MemberIds { get; init; } = [];
    public long FoundedTick { get; set; }
    public GameEntityId? LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public string GovernmentType { get; set; } = "Informal Community";
    // TG-580_Politics_Governance.md names authority sources (competence,
    // tradition, inheritance, election, religious legitimacy, military
    // success) and Legitimacy (public trust, competence, justice, stability)
    // but gives no formula. GovernanceService derives AuthoritySource from
    // GovernmentType and Legitimacy from the leader's real ContributionScore/
    // Reputation plus time-since-last-transition - only 3 of the 6 spec'd
    // authority sources are reachable this way (no inheritance/religious/
    // military path exists yet), and "justice" is omitted entirely since
    // TG-590 Law & Justice is unimplemented. Documented gap, not an oversight.
    public string AuthoritySource { get; set; } = "Competence";
    public double Legitimacy { get; set; } = 50.0;
    public long LastGovernmentChangeTick { get; set; }
    public List<CulturalTrait> CulturalTraits { get; init; } = [];
    public GameEntityId? ReligionId { get; set; }
    public string ReligionName { get; set; } = string.Empty;
    public double TechnologyProgress { get; set; }
    public GameEntityId? KingdomId { get; set; }
    // RFC-007 (specification/RFC/RFC-007-borders-territorial-influence.md):
    // TG-620's regional-influence-field model, computed from the two
    // per-settlement inputs that already exist (Population, Legitimacy) -
    // no formula given in the spec. TerritorySystem owns all updates.
    public double TerritorialInfluence { get; set; }

    public int CompletedBuildings => Buildings.Count(b => b.Status == BuildingStatus.Completed);
    public int UnderConstructionBuildings => Buildings.Count(b => b.Status == BuildingStatus.UnderConstruction);

    public SettlementTier Tier => Population switch
    {
        >= 300 => SettlementTier.Metropolis,
        >= 150 => SettlementTier.RegionalCapital,
        >= 75 => SettlementTier.City,
        >= 30 => SettlementTier.Town,
        >= 10 => SettlementTier.Village,
        _ => SettlementTier.Hamlet
    };

    public bool IsWithinTerritory(int x, int y) =>
        Math.Abs(x - TileX) <= TerritoryRadius && Math.Abs(y - TileY) <= TerritoryRadius;

    public bool HasAvailableHousing =>
        Buildings
            .Where(b => b.BuildingType is "Shelter" or "House" && b.Status == BuildingStatus.Completed)
            .Sum(b => BuildingTypes.GetMaxOccupants(b.BuildingType)) > MemberIds.Count;
}
