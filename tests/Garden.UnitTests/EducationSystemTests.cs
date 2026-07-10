using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 8: Apprenticeship per
/// specification/RFC/RFC-004-education-apprenticeship.md. A mentor
/// (Adult/Elder, higher Intelligence) pairs with a student (Child/Teen,
/// lower Intelligence) only via an existing close Relationship, gradually
/// raising the student's Intelligence toward the mentor's.
/// </summary>
public class EducationSystemTests
{
    private static (WorldState world, EventBus bus, EducationSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new EducationSystem(world, bus);
        return (world, bus, system);
    }

    private static Citizen AddCitizen(WorldState world, string name, LifeStage stage, double intelligence)
    {
        var citizen = new Citizen { FirstName = name, LastName = "Citizen", IsAlive = true, Stage = stage };
        citizen.Attributes.Intelligence = intelligence;
        world.Citizens.Add(citizen);
        return citizen;
    }

    private static void AddCloseRelationship(WorldState world, Citizen a, Citizen b, double socialDistance = 20.0)
    {
        world.Relationships.Add(new Relationship
        {
            EntityAId = a.Id,
            EntityBId = b.Id,
            SocialDistance = socialDistance,
            Trust = 60.0,
            Affection = 60.0
        });
    }

    [Fact]
    public void NoApprenticeship_ExistsByDefault_WithNoRelationship()
    {
        var (world, _, system) = CreateHarness();
        AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        AddCitizen(world, "Student", LifeStage.Child, 3.0);

        system.Execute();

        Assert.Empty(world.Apprenticeships);
    }

    [Fact]
    public void FormsApprenticeship_ForAdultMentor_AndChildStudent_WhenClose()
    {
        var (world, bus, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        var student = AddCitizen(world, "Student", LifeStage.Child, 3.0);
        AddCloseRelationship(world, mentor, student);

        var started = false;
        bus.Subscribe<ApprenticeshipStartedEvent>(_ => started = true);

        system.Execute();

        var apprenticeship = Assert.Single(world.Apprenticeships);
        Assert.Equal(mentor.Id, apprenticeship.MentorId);
        Assert.Equal(student.Id, apprenticeship.StudentId);
        Assert.True(apprenticeship.IsActive);
        Assert.True(started);
    }

    [Fact]
    public void DoesNotForm_WhenBothCitizensAreAdults()
    {
        var (world, _, system) = CreateHarness();
        var a = AddCitizen(world, "A", LifeStage.Adult, 9.0);
        var b = AddCitizen(world, "B", LifeStage.Adult, 3.0);
        AddCloseRelationship(world, a, b);

        system.Execute();

        Assert.Empty(world.Apprenticeships);
    }

    [Fact]
    public void DoesNotForm_WhenIntelligenceGapTooSmall()
    {
        var (world, _, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 5.5);
        var student = AddCitizen(world, "Student", LifeStage.Child, 5.0); // gap 0.5 < 2.0 threshold
        AddCloseRelationship(world, mentor, student);

        system.Execute();

        Assert.Empty(world.Apprenticeships);
    }

    [Fact]
    public void DoesNotForm_WhenSocialDistanceTooFar()
    {
        var (world, _, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        var student = AddCitizen(world, "Student", LifeStage.Child, 3.0);
        AddCloseRelationship(world, mentor, student, socialDistance: 90.0);

        system.Execute();

        Assert.Empty(world.Apprenticeships);
    }

    [Fact]
    public void Intelligence_TransfersGradually_TowardMentor_NeverExceedingIt()
    {
        var (world, _, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        var student = AddCitizen(world, "Student", LifeStage.Child, 3.0);
        AddCloseRelationship(world, mentor, student);

        for (var year = 1; year <= 20; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * 336);
            system.Execute();
        }

        Assert.True(student.Attributes.Intelligence > 3.0,
            $"Expected Intelligence to rise via apprenticeship, but stayed at {student.Attributes.Intelligence}");
        Assert.True(student.Attributes.Intelligence <= mentor.Attributes.Intelligence,
            $"Expected Intelligence to never exceed the mentor's, but reached {student.Attributes.Intelligence}");
    }

    [Fact]
    public void Completes_WhenIntelligenceGapClosesEnough()
    {
        var (world, bus, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        var student = AddCitizen(world, "Student", LifeStage.Child, 3.0);
        AddCloseRelationship(world, mentor, student);

        var completedCount = 0;
        bus.Subscribe<ApprenticeshipCompletedEvent>(_ => completedCount++);

        Apprenticeship? apprenticeship = null;
        for (var year = 1; year <= 30 && (apprenticeship == null || apprenticeship.IsActive); year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * 336);
            system.Execute();
            apprenticeship = world.Apprenticeships.SingleOrDefault();
        }

        Assert.NotNull(apprenticeship);
        Assert.False(apprenticeship!.IsActive);
        Assert.NotNull(apprenticeship.CompletedTick);
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void Completes_WhenRelationshipDecaysPastThreshold()
    {
        var (world, bus, system) = CreateHarness();
        var mentor = AddCitizen(world, "Mentor", LifeStage.Adult, 9.0);
        var student = AddCitizen(world, "Student", LifeStage.Child, 3.0);
        var rel = new Relationship
        {
            EntityAId = mentor.Id,
            EntityBId = student.Id,
            SocialDistance = 20.0,
            Trust = 60.0,
            Affection = 60.0
        };
        world.Relationships.Add(rel);

        system.Execute();
        Assert.Single(world.Apprenticeships);

        // The bond that enabled teaching no longer exists.
        rel.SocialDistance = 95.0;

        var completed = false;
        bus.Subscribe<ApprenticeshipCompletedEvent>(_ => completed = true);
        world.CurrentTime = SimulationTime.FromTick(336);
        system.Execute();

        Assert.True(completed);
        Assert.False(world.Apprenticeships.Single().IsActive);
    }
}
