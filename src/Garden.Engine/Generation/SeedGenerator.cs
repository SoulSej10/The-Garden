namespace Garden.Engine.Generation;

public static class SeedGenerator
{
    public static int FromString(string input)
    {
        var hash = input.Aggregate(0, (current, c) => current * 31 + c);
        return Math.Abs(hash);
    }

    public static int Random() => Math.Abs(Environment.TickCount);
}
