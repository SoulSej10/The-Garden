using Garden.Engine.Services;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AssistantController : ControllerBase
{
    private readonly NarrationService _narrationService;

    public AssistantController(NarrationService narrationService)
    {
        _narrationService = narrationService;
    }

    // RFC-018: tries AI-enhanced narration first, falling back to the
    // deterministic template narrative when no AI provider is configured
    // or the request fails - see NarrationService.GenerateSummaryAsync.
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _narrationService.GenerateSummaryAsync(ct);
        return Ok(summary);
    }

    [HttpPost("question")]
    public IActionResult AskQuestion([FromBody] QuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { Error = "Question is required" });

        var answer = _narrationService.AnswerQuestion(request.Question);
        return Ok(new { Question = request.Question, Answer = answer, Timestamp = DateTime.UtcNow });
    }
}

public record QuestionRequest(string Question);
