using System.Text.Json;

namespace AIReception.Mvc.Services;

public class ReceptionConfigService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReceptionConfigService> _logger;
    private ReceptionConfigRoot? _cached;

    public ReceptionConfigService(IWebHostEnvironment environment, ILogger<ReceptionConfigService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public ReceptionConfigRoot GetConfig()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var path = Path.Combine(_environment.ContentRootPath, "Data", "ait_reception_config.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Reception config file not found at {Path}", path);
            _cached = new ReceptionConfigRoot();
            return _cached;
        }

        var json = File.ReadAllText(path);
        _cached = JsonSerializer.Deserialize<ReceptionConfigRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ReceptionConfigRoot();

        return _cached;
    }

    public LocalizedReply? GetReply(string group, string key)
    {
        var config = GetConfig();
        if (config.Responses.TryGetValue(group, out var groupItems) && groupItems.TryGetValue(key, out var reply))
        {
            return reply;
        }

        return null;
    }
}

public class ReceptionConfigRoot
{
    public string Version { get; set; } = string.Empty;
    public MetaConfig Meta { get; set; } = new();
    public List<IntentDefinition> Intents { get; set; } = new();
    public Dictionary<string, Dictionary<string, LocalizedReply>> Responses { get; set; } = new();
    public Dictionary<string, ObjectionDefinition> Objections { get; set; } = new();
}

public class MetaConfig
{
    public string Name { get; set; } = string.Empty;
    public List<string> LocaleSupport { get; set; } = new();
    public string DefaultLanguage { get; set; } = "fr-CA";
    public string DefaultDepartment { get; set; } = "service";
    public string SafeEscalationRule { get; set; } = string.Empty;
}

public class IntentDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public bool Bookable { get; set; }
    public List<string> Triggers { get; set; } = new();
    public List<string> EntitiesToCapture { get; set; } = new();
}

public class LocalizedReply
{
    public string En { get; set; } = string.Empty;
    public string Fr { get; set; } = string.Empty;
}

public class ObjectionDefinition
{
    public List<string> CustomerSignals { get; set; } = new();
    public LocalizedReply Response { get; set; } = new();
}
