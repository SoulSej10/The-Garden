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

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var summary = _narrationService.GenerateSummary();
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
