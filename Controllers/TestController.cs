using Microsoft.AspNetCore.Mvc;
using AIReception.Mvc.Services;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    private readonly DirectoryRoutingService _directory;
    private readonly RuntimeRulesService _runtime;

    public TestController(
        DirectoryRoutingService directory,
        RuntimeRulesService runtime)
    {
        _directory = directory;
        _runtime = runtime;
    }

    [HttpGet("directory")]
    public IActionResult Directory()
    {
        var entries = _directory.GetAll();

        return Ok(new
        {
            count = entries.Count,
            sample = entries.Take(10)
        });
    }

    [HttpGet("directory-match")]
    public IActionResult DirectoryMatch([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var exactEmployee = _directory.FindEmployee(q);
        var bestMatch = _directory.FindBestMatch(q);
        var detectedLanguage = _runtime.DetectLanguage(q);
        var intent = _runtime.FindIntent(q);

        return Ok(new
        {
            query = q,
            language = detectedLanguage,
            intent = intent?.Id,
            exactEmployee = exactEmployee?.Name,
            bestMatch = bestMatch == null
                ? null
                : new
                {
                    name = bestMatch.Entry.Name,
                    type = bestMatch.Entry.Type,
                    department = bestMatch.Entry.Department,
                    phone = bestMatch.Entry.PhoneE164,
                    score = bestMatch.Score,
                    matchedOn = bestMatch.MatchedOn
                }
        });
    }

    [HttpGet("routing-score")]
    public IActionResult RoutingScore([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var match = _directory.FindBestMatch(q);

        if (match == null)
        {
            return Ok(new
            {
                query = q,
                score = 0.0,
                confidence = "none",
                route = "fallback"
            });
        }

        var confidence =
            match.Score >= 0.7 ? "high" :
            match.Score >= 0.4 ? "medium" :
            "low";

        return Ok(new
        {
            query = q,
            name = match.Entry.Name,
            department = match.Entry.Department,
            phone = match.Entry.PhoneE164,
            score = match.Score,
            matchedOn = match.MatchedOn,
            confidence,
            route = confidence == "low" ? "fallback" : "transfer"
        });
    }
}
