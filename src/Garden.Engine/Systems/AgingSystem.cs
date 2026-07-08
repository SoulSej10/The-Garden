using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class AgingSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AgingSystem> _logger;
    private long _nextExecutionTick;

    public string Name => "AgingSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public AgingSystem(WorldState worldState, IEventBus eventBus, ILogger<AgingSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var citizen in _worldState.Citizens.Where(c => c.IsAlive))
        {
            // Whole years elapsed since birth, measured in ticks rather than
            // calendar-year subtraction (which rounded newborns down to 0
            // whenever birth and current tick fell in the same in-game year).
            var newAge = (int)Math.Max(0, (tick - citizen.BirthTick) / (24L * 30 * 12));

            if (newAge != citizen.Age)
            {
                citizen.Age = newAge;
                citizen.Stage = GetLifeStage(newAge);

                _eventBus.Publish(new CitizenAgedEvent
                {
                    Tick = tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                    NewAge = newAge,
                    LifeStage = citizen.Stage.ToString()
                });

                ApplyAgingEffects(citizen);
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private static LifeStage GetLifeStage(int age)
    {
        return age switch
        {
            < 2 => LifeStage.Newborn,
            < 13 => LifeStage.Child,
            < 18 => LifeStage.Teen,
            < 60 => LifeStage.Adult,
            _ => LifeStage.Elder
        };
    }

    private static void ApplyAgingEffects(Citizen citizen)
    {
        var ageFactor = citizen.Age / 80.0;
        citizen.Attributes.Strength = Math.Max(1, 5 * (1 - ageFactor * 0.5));
        citizen.Attributes.Endurance = Math.Max(1, 5 * (1 - ageFactor * 0.4));
    }
}
