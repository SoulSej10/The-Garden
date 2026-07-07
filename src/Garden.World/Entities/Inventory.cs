using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Inventory
{
    public List<ItemStack> Items { get; init; } = [];
    public double MaxCapacity { get; set; } = 100;

    public double CurrentWeight => Items.Sum(i => i.Quantity * i.Weight);

    public void Add(string itemType, double quantity, double weight = 1.0)
    {
        var existing = Items.FirstOrDefault(i => i.ItemType == itemType);
        if (existing != null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, existing.StackLimit);
        }
        else
        {
            Items.Add(new ItemStack
            {
                Id = GameEntityId.New(),
                ItemType = itemType,
                Quantity = quantity,
                Weight = weight
            });
        }
    }

    public double Remove(string itemType, double quantity)
    {
        var existing = Items.FirstOrDefault(i => i.ItemType == itemType);
        if (existing == null) return 0;

        var removed = Math.Min(quantity, existing.Quantity);
        existing.Quantity -= removed;
        return removed;
    }

    public double GetQuantity(string itemType) =>
        Items.Where(i => i.ItemType == itemType).Sum(i => i.Quantity);

    public bool HasCapacityFor(double weight) => CurrentWeight + weight <= MaxCapacity;
}

public class ItemStack
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string ItemType { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double Weight { get; set; } = 1.0;
    public double StackLimit { get; set; } = 100;
}
