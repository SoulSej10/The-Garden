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
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var stories = _storyEngine.Stories
            .OrderByDescending(s => s.Tick)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalStories = _storyEngine.Stories.Count,
            Stories = stories.Select(s => new
            {
                s.Id, s.Tick, s.Category, s.Title, s.Summary,
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
            story.Id, story.Tick, story.Category, story.Title,
            story.Summary, story.Narrative, story.ParticipantNames,
            story.RelatedRecordIds, story.GeneratedAtTick
        });
    }
}
