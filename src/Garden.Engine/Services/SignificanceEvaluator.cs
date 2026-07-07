using Garden.World.Entities;

namespace Garden.Engine.Services;

public class SignificanceEvaluator
{
    private static readonly HashSet<string> HighImportanceEvents =
    [
        "CitizenBorn", "CitizenDied", "SettlementFounded", "SettlementExpanded",
        "BuildingCompleted", "FarmHarvested", "TradeCompleted",
        "SettlementAbandoned", "CitizenMurdered", "WarDeclared",
        "MajorFlood", "LongDrought", "Earthquake", "VolcanicEruption",
        "Plague", "Wildfire"
    ];

    private static readonly HashSet<string> MediumImportanceEvents =
    [
        "BuildingPlanned", "ResourceGathered", "GoodsCrafted",
        "FarmPlanted", "CitizenAged"
    ];

    public string Evaluate(string eventType, double? severity = null)
    {
        if (HighImportanceEvents.Contains(eventType))
            return "High";

        if (MediumImportanceEvents.Contains(eventType))
            return "Medium";

        if (severity.HasValue && severity.Value > 7.0)
            return "High";
        if (severity.HasValue && severity.Value > 4.0)
            return "Medium";

        return "Low";
    }

    public bool ShouldArchive(string eventType, string importance)
    {
        if (importance == "High" || importance == "Medium")
            return true;

        return false;
    }
}
