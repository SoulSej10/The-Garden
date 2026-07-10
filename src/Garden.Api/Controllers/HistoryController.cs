using Garden.Engine.Services;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HistoryController : ControllerBase
{
    private readonly HistoryManager _historyManager;
    private readonly HistoricalArchive _archive;
    private readonly TimelineService _timeline;

    public HistoryController(HistoryManager historyManager, HistoricalArchive archive, TimelineService timeline)
    {
        _historyManager = historyManager;
        _archive = archive;
        _timeline = timeline;
    }

    [HttpGet]
    public IActionResult GetHistory()
    {
        var records = _archive.Records;
        var (births, deaths) = _archive.GetVitalStats();

        return Ok(new
        {
            TotalRecords = _archive.Count,
            TotalBirths = births,
            TotalDeaths = deaths,
            EventTypeCounts = _archive.GetEventTypeCounts(),
            Records = records.TakeLast(100).Reverse().Select(r => new
            {
                Id = r.Id.ToString(), r.Tick, r.Year, r.Day, r.Season,
                r.EventType, r.Category, r.Title, r.Description,
                r.LocationX, r.LocationY, r.LocationName,
                r.ParticipantNames, r.Severity, r.Importance
            })
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var record = _archive.GetById(id);
        if (record == null) return NotFound();
        return Ok(new
        {
            Id = record.Id.ToString(), record.Tick, record.Year, record.Day, record.Season,
            record.EventType, record.Category, record.Title, record.Description,
            record.LocationX, record.LocationY, record.LocationName,
            record.ParticipantIds, record.ParticipantNames,
            record.RelatedSettlementId, record.RelatedEventIds,
            record.Severity, record.SourceEventIds, record.Importance
        });
    }

    [HttpGet("timeline")]
    public IActionResult GetTimeline(
        [FromQuery] int? fromTick,
        [FromQuery] int? toTick,
        [FromQuery] string? category,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = _timeline.GetTimeline(fromTick, toTick, category, eventType, page, pageSize);
        return Ok(result);
    }

    [HttpGet("search")]
    public IActionResult Search(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? eventType,
        [FromQuery] int? fromTick,
        [FromQuery] int? toTick,
        [FromQuery] string? participantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var records = _archive.Search(
            eventType: eventType,
            category: category,
            fromTick: fromTick,
            toTick: toTick,
            keyword: q,
            participantId: participantId,
            skip: (page - 1) * pageSize,
            take: pageSize
        );

        var total = _archive.Search(
            eventType: eventType,
            category: category,
            fromTick: fromTick,
            toTick: toTick,
            keyword: q,
            participantId: participantId
        ).Count;

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Records = records.Select(r => new
            {
                Id = r.Id.ToString(), r.Tick, r.Year, r.Day, r.Season,
                r.EventType, r.Category, r.Title, r.Description,
                r.LocationX, r.LocationY, r.LocationName,
                r.ParticipantNames, r.Severity, r.Importance
            })
        });
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var (births, deaths) = _archive.GetVitalStats();
        var memoryStats = _historyManager.Memory.GetMemoryStats();

        return Ok(new
        {
            TotalRecords = _archive.Count,
            Births = births,
            Deaths = deaths,
            EventTypeCounts = _archive.GetEventTypeCounts(),
            MemoryStats = memoryStats,
            StoryCount = _historyManager.StoryEngine.Stories.Count
        });
    }
}
