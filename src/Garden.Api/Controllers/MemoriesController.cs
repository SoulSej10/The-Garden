using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("citizens/{id}/memories")]
public class MemoriesController : ControllerBase
{
    private readonly MemoryService _memoryService;
    private readonly WorldState _worldState;

    public MemoriesController(MemoryService memoryService, WorldState worldState)
    {
        _memoryService = memoryService;
        _worldState = worldState;
    }

    [HttpGet]
    public IActionResult GetCitizenMemories(Guid id)
    {
        var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id.Value == id);
        if (citizen == null) return NotFound();

        var memories = _memoryService.GetCitizenMemories(id.ToString());
        return Ok(new
        {
            CitizenId = id,
            CitizenName = $"{citizen.FirstName} {citizen.LastName}",
            TotalMemories = memories.Count,
            Memories = memories.Select(m => new
            {
                Id = m.Id.ToString(), m.Tick, m.EventType, m.Title, m.Description,
                m.Confidence, m.EmotionalImpact
            })
        });
    }

    [HttpGet("family")]
    public IActionResult GetFamilyMemories(Guid id)
    {
        var memories = _memoryService.GetFamilyMemories(id.ToString());
        return Ok(new
        {
            CitizenId = id,
            TotalMemories = memories.Count,
            Memories = memories.Select(m => new
            {
                Id = m.Id.ToString(), m.Tick, m.EventType, m.Title, m.Description,
                m.AncestorIds
            })
        });
    }

    [HttpGet("collective")]
    public IActionResult GetCollectiveMemories(Guid id)
    {
        var memories = _memoryService.GetCollectiveMemories(id.ToString());
        return Ok(new
        {
            CitizenId = id,
            TotalMemories = memories.Count,
            Memories = memories.Select(m => new
            {
                Id = m.Id.ToString(), m.Tick, m.EventType, m.Title, m.Description,
                m.Importance
            })
        });
    }
}
