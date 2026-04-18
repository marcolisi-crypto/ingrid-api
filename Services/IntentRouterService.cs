namespace AIReception.Mvc.Services;

public class IntentRouterService
{
    private readonly ReceptionConfigService _configService;

    public IntentRouterService(ReceptionConfigService configService)
    {
        _configService = configService;
    }

    public IntentMatchResult? DetectIntent(string input)
    {
        var normalized = (input ?? string.Empty).ToLowerInvariant();
        var config = _configService.GetConfig();

        foreach (var intent in config.Intents)
        {
            if (intent.Triggers.Any(t => normalized.Contains(t.ToLower())))
            {
                return new IntentMatchResult
                {
                    IntentId = intent.Id,
                    Department = intent.Department,
                    Priority = intent.Priority
                };
            }
        }

        return null;
    }

    public string? GetIntentResponse(string intentId, string language)
    {
        var config = _configService.GetConfig();

        if (config.Responses.TryGetValue("service", out var serviceGroup))
        {
            if (intentId.Contains("maintenance") && serviceGroup.TryGetValue("bookMaintenance", out var reply))
            {
                return language == "fr-CA" ? reply.Fr : reply.En;
            }

            if (intentId.Contains("diagnostic") && serviceGroup.TryGetValue("bookDiagnostic", out var diag))
            {
                return language == "fr-CA" ? diag.Fr : diag.En;
            }
        }

        return null;
    }
}

public class IntentMatchResult
{
    public string IntentId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}
