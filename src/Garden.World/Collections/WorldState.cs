using Garden.Core.Time;
using Garden.World.Entities;

namespace Garden.World.Collections;

public class WorldState
{
    public SimulationTime CurrentTime { get; set; }
    public List<Citizen> Citizens { get; } = [];
    public List<Settlement> Settlements { get; } = [];
    public double GlobalFood { get; set; }
    public double GlobalWood { get; set; }
    public double GlobalStone { get; set; }
}
