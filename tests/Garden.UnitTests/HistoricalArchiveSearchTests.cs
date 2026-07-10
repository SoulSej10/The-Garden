using Garden.Engine.Services;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 4 Day 17: faceted search per TG-OBS-005's
/// "Historical Search" section - covers Settlement (new settlementId param)
/// and Person (keyword now also matching participant names, not just
/// title/description).
/// </summary>
public class HistoricalArchiveSearchTests
{
    private static HistoricalRecord MakeRecord(
        string title, string description, string settlementId = "",
        List<string>? participantNames = null, long tick = 0)
    {
        return new HistoricalRecord
        {
            Tick = tick,
            Title = title,
            Description = description,
            RelatedSettlementId = settlementId,
            ParticipantNames = participantNames ?? [],
            Category = HistoryCategories.Event,
            EventType = "Test"
        };
    }

    [Fact]
    public void Search_FiltersBySettlementId_WhenProvided()
    {
        var archive = new HistoricalArchive();
        archive.Append(MakeRecord("A", "desc", settlementId: "settlement-1"));
        archive.Append(MakeRecord("B", "desc", settlementId: "settlement-2"));

        var results = archive.Search(settlementId: "settlement-1");

        var record = Assert.Single(results);
        Assert.Equal("A", record.Title);
    }

    [Fact]
    public void Search_WithNoSettlementIdFilter_ReturnsAllSettlements()
    {
        var archive = new HistoricalArchive();
        archive.Append(MakeRecord("A", "desc", settlementId: "settlement-1"));
        archive.Append(MakeRecord("B", "desc", settlementId: "settlement-2"));

        var results = archive.Search();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_KeywordMatchesParticipantName_NotJustTitleOrDescription()
    {
        var archive = new HistoricalArchive();
        archive.Append(MakeRecord("Unrelated Title", "Unrelated description",
            participantNames: ["Una Mosswood"]));
        archive.Append(MakeRecord("Something Else", "Also unrelated",
            participantNames: ["Toma Fielding"]));

        var results = archive.Search(keyword: "Mosswood");

        var record = Assert.Single(results);
        Assert.Equal("Unrelated Title", record.Title);
    }

    [Fact]
    public void Search_KeywordStillMatchesTitleAndDescription_AsBefore()
    {
        var archive = new HistoricalArchive();
        archive.Append(MakeRecord("Founding of Rivermoot", "A settlement was founded"));
        archive.Append(MakeRecord("Unrelated", "Also unrelated"));

        var results = archive.Search(keyword: "Rivermoot");

        var record = Assert.Single(results);
        Assert.Equal("Founding of Rivermoot", record.Title);
    }

    [Fact]
    public void Search_CombinesSettlementAndKeywordFilters()
    {
        var archive = new HistoricalArchive();
        archive.Append(MakeRecord("Founding", "desc", settlementId: "settlement-1",
            participantNames: ["Una Mosswood"]));
        archive.Append(MakeRecord("Founding", "desc", settlementId: "settlement-2",
            participantNames: ["Una Mosswood"]));

        var results = archive.Search(keyword: "Mosswood", settlementId: "settlement-1");

        var record = Assert.Single(results);
        Assert.Equal("settlement-1", record.RelatedSettlementId);
    }
}
