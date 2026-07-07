namespace Garden.Core.Time;

public readonly record struct SimulationTime
{
    public long Tick { get; init; }
    public int Hour => (int)(Tick % 24);
    public int Day => (int)(Tick / 24) % 30 + 1;
    public int Month => (int)(Tick / (24 * 30)) % 12 + 1;
    public int Year => (int)(Tick / (24 * 30 * 12)) + 1;
    public Season Season => (Season)((Month - 1) / 3);

    public static SimulationTime FromTick(long tick) => new() { Tick = tick };
    public static SimulationTime FromYmd(int year, int month, int day, int hour = 0)
    {
        var ticks = (year - 1) * 24L * 30 * 12 + (month - 1) * 24L * 30 + (day - 1) * 24L + hour;
        return new SimulationTime { Tick = ticks };
    }

    public SimulationTime AddTicks(long ticks) => new() { Tick = Tick + ticks };
    public SimulationTime AddHours(int hours) => AddTicks(hours);
    public SimulationTime AddDays(int days) => AddTicks(days * 24);
    public SimulationTime AddYears(int years) => AddTicks(years * 24L * 30 * 12);

    public override string ToString() => $"Year {Year}, {Season} {Day}, Hour {Hour}:00";
}

public enum Season
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}
