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
    public List<CitizenMemory> Memories { get; init; } = [];
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
    public double Strength { get; set; } = 5.0;
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

public class CitizenMemory
{
    public long Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
