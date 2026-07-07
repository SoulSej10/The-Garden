using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class CultureService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CultureService> _logger;
    private int _festivalNameIndex;

    private static readonly string[] FestivalPrefixes =
        ["Spring", "Summer", "Harvest", "Winter", "Moon", "Sun", "Star", "Dawn", "Twilight", "Fire"];
    private static readonly string[] FestivalSuffixes =
        ["Festival", "Celebration", "Gathering", "Fair", "Feast", "Ceremony", "Rite"];

    public CultureService(WorldState worldState, IEventBus eventBus, ILogger<CultureService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateCulture(long tick)
    {
        foreach (var settlement in _worldState.Settlements.Where(s => s.MemberIds.Count >= 3))
        {
            var currentTraits = settlement.CulturalTraits.ToList();
            var existingCount = currentTraits.Count;

            if (existingCount < 2 && System.Random.Shared.NextDouble() < 0.05)
            {
                var available = CulturalTraitTemplates.All
                    .Where(t => currentTraits.All(ct => ct.Name != t.Name))
                    .ToList();

                if (available.Count > 0)
                {
                    var weighted = available
                        .Select(t => new
                        {
                            Trait = t,
                            Weight = GetTraitWeight(t, settlement)
                        })
                        .OrderByDescending(x => x.Weight)
                        .ToList();

                    var chosen = weighted[0].Trait;
                    settlement.CulturalTraits.Add(new CulturalTrait
                    {
                        Name = chosen.Name,
                        Description = chosen.Description,
                        Category = chosen.Category,
                        EstablishedTick = tick
                    });

                    _logger.LogDebug("{Settlement} developed cultural trait: {Trait}",
                        settlement.Name, chosen.Name);
                }
            }

            if (settlement.MemberIds.Count >= 5 && System.Random.Shared.NextDouble() < 0.02)
            {
                var festivalName = GenerateFestivalName();
                var participants = Math.Min(settlement.MemberIds.Count, System.Random.Shared.Next(3, settlement.MemberIds.Count + 1));

                _eventBus.Publish(new CulturalFestivalHeldEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    FestivalName = festivalName,
                    Occasion = settlement.CulturalTraits.Count > 0
                        ? $"{settlement.CulturalTraits[0].Name} tradition"
                        : "Community gathering",
                    ParticipantCount = participants
                });
            }
        }
    }

    private static double GetTraitWeight(CulturalTrait trait, Settlement settlement)
    {
        var weight = 1.0;

        if (trait.Category == "Economic" && settlement.Storage.Items.Sum(i => i.Quantity) > 50) weight += 2;
        if (trait.Category == "Spiritual" && settlement.Buildings.Count(b => b.Status == BuildingStatus.Completed) > 3) weight += 1;
        if (trait.Category == "Social" && settlement.MemberIds.Count > 8) weight += 2;
        if (trait.Category == "Knowledge" && settlement.TechnologyProgress > 50) weight += 2;
        if (trait.Category == "Military") weight += 0.5;
        if (trait.Category == "Art" && settlement.Storage.GetQuantity("Food") > 100) weight += 1.5;

        return weight;
    }

    private string GenerateFestivalName()
    {
        var p = FestivalPrefixes[_festivalNameIndex % FestivalPrefixes.Length];
        var s = FestivalSuffixes[_festivalNameIndex / FestivalPrefixes.Length % FestivalSuffixes.Length];
        _festivalNameIndex++;
        return $"{p} {s}";
    }
}
