using System.Text.Json;

namespace AIReception.Mvc.Services;

public class RuntimeRulesService
{
    private readonly IConfiguration _config;
    private readonly ILogger<RuntimeRulesService> _logger;

    private List<RuntimeIntentDefinition> _intents = new();

    public RuntimeRulesService(IConfiguration config, ILogger<RuntimeRulesService> logger)
    {
        _config = config;
        _logger = logger;
        LoadConfig();
    }

    private void LoadConfig()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ait_reception_config.json");

        if (!File.Exists(path))
        {
            _logger.LogError("Config file not found: {Path}", path);
            return;
        }

        var json = File.ReadAllText(path);

        var doc = JsonSerializer.Deserialize<RuntimeConfigRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (doc?.Intents != null)
        {
            _intents = doc.Intents;
            _logger.LogInformation("Loaded {Count} intents from config", _intents.Count);
        }
    }

    public RuntimeIntentDefinition? FindIntent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = Normalize(input);

        foreach (var intent in _intents)
        {
            foreach (var trigger in intent.Triggers)
            {
                var normalizedTrigger = Normalize(trigger);

                if (string.IsNullOrWhiteSpace(normalizedTrigger))
                    continue;

                if (normalized.Contains(normalizedTrigger))
                {
                    _logger.LogInformation(
                        "Intent matched: {IntentId} using trigger '{Trigger}' for input '{Input}'",
                        intent.Id,
                        trigger,
                        input);

                    return intent;
                }

                if (normalizedTrigger.Contains(normalized))
                {
                    _logger.LogInformation(
                        "Intent reverse-matched: {IntentId} using trigger '{Trigger}' for input '{Input}'",
                        intent.Id,
                        trigger,
                        input);

                    return intent;
                }
            }
        }

        return null;
    }

    public string DetectLanguage(string input)
    {
        var normalized = Normalize(input);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "fr-CA";
        }

        string[] frenchMarkers =
        {
            "bonjour",
            "bonsoir",
            "francais",
            "français",
            "en francais",
            "en français",
            "rendez vous",
            "voiture",
            "huile",
            "entretien",
            "reparation",
            "réparation",
            "mecanique",
            "mécanique",
            "pneu",
            "crevaison",
            "pieces",
            "pièces",
            "ventes",
            "paiement",
            "garantie",
            "location",
            "bonjour",
            "je",
            "voudrais",
            "aimerais",
            "parler",
            "avec",
            "besoin",
            "auto",
            "ma voiture",
            "mon auto",
            "prendre rendez vous",
            "prendre rendez-vous",
            "changer huile",
            "changement d huile",
            "changement d'huile",
            "frein",
            "voyant",
            "probleme",
            "problème",
            "aide"
        };

        string[] englishMarkers =
        {
            "hello",
            "hi",
            "english",
            "in english",
            "appointment",
            "oil change",
            "flat tire",
            "puncture",
            "service appointment",
            "sales department",
            "parts department",
            "finance",
            "repair",
            "maintenance",
            "car",
            "vehicle",
            "book",
            "schedule",
            "reschedule",
            "speak to",
            "talk to",
            "need help",
            "my car",
            "check engine",
            "warning light",
            "brake",
            "loaner",
            "lease",
            "payment",
            "used car",
            "new car",
            "inventory"
        };

        var frenchScore = frenchMarkers.Count(m => normalized.Contains(m));
        var englishScore = englishMarkers.Count(m => normalized.Contains(m));

        _logger.LogInformation(
            "Language detection for '{Input}' => frScore={FrenchScore}, enScore={EnglishScore}",
            input,
            frenchScore,
            englishScore);

        if (englishScore > frenchScore)
        {
            return "en-US";
        }

        return "fr-CA";
    }

    private string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = new string(
            text
                .ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

        return cleaned.Trim();
    }
}

public class RuntimeConfigRoot
{
    public List<RuntimeIntentDefinition> Intents { get; set; } = new();
}

public class RuntimeIntentDefinition
{
    public string Id { get; set; } = "";
    public string Department { get; set; } = "";
    public List<string> Triggers { get; set; } = new();
}
