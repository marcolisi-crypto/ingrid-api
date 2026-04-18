using System.Text.Json.Serialization;

namespace AIReception.Mvc.Models.Directory;

public class DirectoryEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    [JsonPropertyName("phone_e164")]
    public string PhoneE164 { get; set; } = string.Empty;

    [JsonPropertyName("phone_display")]
    public string? PhoneDisplay { get; set; }

    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    [JsonPropertyName("site")]
    public string? Site { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}
