namespace AIReception.Mvc.Models.Memory;

public class CallerMemory
{
    public string PhoneNumber { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = "fr-CA";

    public string LastDepartment { get; set; } = string.Empty;

    public string LastAdvisorName { get; set; } = string.Empty;

    public string LastIntentId { get; set; } = string.Empty;

    public string VehicleYear { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}