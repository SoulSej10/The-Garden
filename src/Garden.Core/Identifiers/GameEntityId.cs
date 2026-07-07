namespace Garden.Core.Identifiers;

public readonly record struct GameEntityId(Guid Value)
{
    public static GameEntityId New() => new(Guid.NewGuid());
    public static GameEntityId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
