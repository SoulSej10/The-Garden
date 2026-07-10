using Garden.Engine.Services;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class StoriesController : ControllerBase
{
    private readonly StoryEngine _storyEngine;

    public StoriesController(StoryEngine storyEngine)
    {
        _storyEngine = storyEngine;
    }

    [HttpGet]
    public IActionResult GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? category = null,
        [FromQuery] string? q = null)
    {
        // TG-OBS-005 facets (Day 17): Theme -> category, Person -> keyword
        // match against participant names (Story has no title/description
        // full-text worth searching separately - Summary/Narrative already
        // exist for reading, not filtering).
        var filtered = _storyEngine.Stories.AsEnumerable();
        if (!string.IsNullOrEmpty(category))
            filtered = filtered.Where(s => s.Category == category);
        if (!string.IsNullOrEmpty(q))
            filtered = filtered.Where(s =>
                s.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.ParticipantNames.Any(n => n.Contains(q, StringComparison.OrdinalIgnoreCase)));

        var totalStories = filtered.Count();
        var stories = filtered
            .OrderByDescending(s => s.Tick)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalStories = totalStories,
            Stories = stories.Select(s => new
            {
                Id = s.Id.ToString(), s.Tick, s.Category, s.Title, s.Summary,
                s.ParticipantNames, s.GeneratedAtTick
            })
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var story = _storyEngine.GetById(id);
        if (story == null) return NotFound();

        return Ok(new
        {
            Id = story.Id.ToString(), story.Tick, story.Category, story.Title,
            story.Summary, story.Narrative, story.ParticipantNames,
            story.RelatedRecordIds, story.GeneratedAtTick
        });
    }
}
