using Garden.Core.Identifiers;
using Garden.World.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Garden.Infrastructure.Persistence;

public class GameEntityIdConverter : ValueConverter<GameEntityId, Guid>
{
    public GameEntityIdConverter() : base(
        id => id.Value,
        value => new GameEntityId(value))
    {
    }
}

public class GardenDbContext : DbContext
{
    public GardenDbContext(DbContextOptions<GardenDbContext> options) : base(options) { }

    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<GameEntityId>()
            .HaveConversion<GameEntityIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Citizen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.OwnsOne(e => e.Attributes);
            entity.OwnsOne(e => e.Personality);
            entity.OwnsOne(e => e.Needs);
            entity.OwnsMany(e => e.Memories);
        });

        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.Property(e => e.MemberIds)
                .HasConversion(
                    v => v.Select(id => id.Value).ToList(),
                    v => v.Select(id => new GameEntityId(id)).ToList())
                .Metadata.SetValueComparer(new ValueComparer<List<GameEntityId>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.Value.GetHashCode())),
                    v => v.ToList()));

            entity.OwnsOne(e => e.Storage, storage =>
            {
                storage.OwnsMany(s => s.Items);
            });

            entity.OwnsMany(e => e.Buildings, building =>
            {
                building.HasKey(b => b.Id);
                building.OwnsOne(b => b.Storage, storage =>
                {
                    storage.OwnsMany(s => s.Items);
                });
            });
        });
    }
}
