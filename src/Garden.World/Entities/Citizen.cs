using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Citizen
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string BiologicalSex { get; set; } = "Unknown";
    public LifeStage Stage { get; set; } = LifeStage.Newborn;
    public int TileX { get; set; }
    public int TileY { get; set; }
    public GameEntityId? HomeSettlementId { get; set; }
    public string CurrentActivity { get; set; } = "Idle";
    public string CurrentGoal { get; set; } = "None";
    public bool IsAlive { get; set; } = true;
    public long BirthTick { get; set; }
    public long? DeathTick { get; set; }
    public string CauseOfDeath { get; set; } = string.Empty;

    public CitizenAttributes Attributes { get; set; } = new();
    public PersonalityTraits Personality { get; set; } = new();
    public CitizenNeeds Needs { get; set; } = new();
    public EmotionalState Emotions { get; set; } = new();
    public List<CitizenMemory> Memories { get; init; } = [];

    // RFC-002 (specification/RFC/RFC-002-communication-knowledge-diffusion.md):
    // flat list of HistoricalRecord.Id string values this citizen has heard
    // about, via CommunicationSystem. Deliberately just IDs, not a richer
    // "belief" object - this increment tracks whether knowledge spread, not
    // what shape it took once it arrived (Information Fidelity is deferred).
    public List<string> KnownEventIds { get; init; } = [];

    // Audit finding 3c: throttles the MakeDecision farm-planting pre-emption
    // to once per citizen per day (absolute day = Tick / 24) - without this,
    // a citizen would keep re-planting instead of ever addressing their own
    // hunger/thirst for the rest of the day once seeds dropped back below 20.
    public long LastFarmWorkDay { get; set; } = -1;

    // Rebalancing audit finding 4/5/8: nothing previously distinguished a
    // citizen who survived a prior infection from one who never got sick -
    // every generation faced the exact same unmodified risk forever, with
    // no way for a population to "learn" to survive recurring disease.
    // Incremented by DiseaseSystem on recovery; read as a dampener on both
    // future infection chance and severity growth.
    public double DiseaseResistance { get; set; }

    // Same finding: throttles CitizenSystem's family sick-care goal to once
    // per citizen per day, mirroring LastFarmWorkDay's pattern.
    public long LastCareForFamilyDay { get; set; } = -1;

    public GameEntityId? ParentAId { get; set; }
    public GameEntityId? ParentBId { get; set; }

    public double ContributionScore { get; set; }
    public double Reputation { get; set; } = 50.0;
    public GameEntityId? ReligionId { get; set; }
    public string ReligionName { get; set; } = string.Empty;
}

public enum LifeStage
{
    Newborn,
    Child,
    Teen,
    Adult,
    Elder
}

public class CitizenAttributes
{
    public double Strength { get; set; }
    public double Endurance { get; set; } = 5.0;
    public double Intelligence { get; set; } = 5.0;
    public double Dexterity { get; set; } = 5.0;
    public double Perception { get; set; } = 5.0;
}

public class PersonalityTraits
{
    public double Curiosity { get; set; } = 5.0;
    public double Patience { get; set; } = 5.0;
    public double Aggression { get; set; } = 5.0;
    public double Compassion { get; set; } = 5.0;
    public double Diligence { get; set; } = 5.0;
    public double Introversion { get; set; } = 5.0;
}

public class CitizenNeeds
{
    public double Hunger { get; set; }
    public double Thirst { get; set; }
    public double Energy { get; set; } = 100.0;
    public double Warmth { get; set; } = 50.0;
    public double Health { get; set; } = 100.0;

    public const double MaxHunger = 100.0;
    public const double MaxThirst = 100.0;
    public const double MaxEnergy = 100.0;
    public const double MaxWarmth = 100.0;
    public const double MaxHealth = 100.0;

    public const double HungerWarningThreshold = 60.0;
    public const double ThirstWarningThreshold = 60.0;
    public const double EnergyWarningThreshold = 30.0;
    public const double WarmthWarningThreshold = 30.0;
    public const double HealthWarningThreshold = 40.0;

    public const double HungerCriticalThreshold = 80.0;
    public const double ThirstCriticalThreshold = 80.0;
    public const double EnergyCriticalThreshold = 15.0;
    public const double WarmthCriticalThreshold = 15.0;
    public const double HealthCriticalThreshold = 20.0;
}

// RFC-001 (specification/RFC/RFC-001-emotion-system.md): first increment of
// TG-330_Emotion.md, covering 6 of the 15 named emotions - the ones with a
// real trigger already available in this codebase. All six are 0-100
// doubles, matching every other citizen stat (CitizenNeeds, PersonalityTraits,
// Reputation) rather than a scale TG-330 never specifies. EmotionSystem
// (Garden.Engine/Systems) owns all updates to this state - do not mutate it
// from CitizenSystem or elsewhere.
public class EmotionalState
{
    public double Fear { get; set; }
    public double Joy { get; set; }
    public double Sadness { get; set; }
    public double Trust { get; set; } = 50.0;
    public double Curiosity { get; set; }
    public double Loneliness { get; set; }
}

public class CitizenMemory
{
    public long Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
