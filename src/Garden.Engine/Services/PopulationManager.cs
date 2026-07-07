namespace Garden.Engine.Services;

public class PopulationManager
{
    private int _totalBirths;
    private int _totalDeaths;
    private readonly Dictionary<string, int> _deathCauses = [];

    public int TotalPopulation { get; private set; }
    public int TotalBirths => _totalBirths;
    public int TotalDeaths => _totalDeaths;
    public IReadOnlyDictionary<string, int> DeathCauses => _deathCauses;

    public void RecordBirth() => _totalBirths++;
    public void RecordDeath(string cause)
    {
        _totalDeaths++;
        if (!_deathCauses.ContainsKey(cause))
            _deathCauses[cause] = 0;
        _deathCauses[cause]++;
    }

    public void UpdatePopulation(int count) => TotalPopulation = count;

    public double GetAverageAge(IEnumerable<World.Entities.Citizen> citizens)
    {
        var list = citizens.Where(c => c.IsAlive).ToList();
        return list.Count > 0 ? list.Average(c => c.Age) : 0.0;
    }

    public (int Infants, int Children, int Teens, int Adults, int Elders)
        GetAgeDistribution(IEnumerable<World.Entities.Citizen> citizens)
    {
        var alive = citizens.Where(c => c.IsAlive).ToList();
        return (
            alive.Count(c => c.Stage == World.Entities.LifeStage.Newborn),
            alive.Count(c => c.Stage == World.Entities.LifeStage.Child),
            alive.Count(c => c.Stage == World.Entities.LifeStage.Teen),
            alive.Count(c => c.Stage == World.Entities.LifeStage.Adult),
            alive.Count(c => c.Stage == World.Entities.LifeStage.Elder)
        );
    }
}
