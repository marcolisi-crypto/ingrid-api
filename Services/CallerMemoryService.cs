using System.Collections.Concurrent;
using AIReception.Mvc.Models.Memory;

namespace AIReception.Mvc.Services;

public class CallerMemoryService
{
    private readonly ConcurrentDictionary<string, CallerMemory> _memory = new();

    public CallerMemory? Get(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        _memory.TryGetValue(Normalize(phoneNumber), out var memory);
        return memory;
    }

    public CallerMemory Upsert(
        string phoneNumber,
        string? preferredLanguage = null,
        string? lastDepartment = null,
        string? lastAdvisorName = null,
        string? lastIntentId = null,
        string? vehicleYear = null,
        string? vehicleModel = null)
    {
        var key = Normalize(phoneNumber);

        var updated = _memory.AddOrUpdate(
            key,
            _ => new CallerMemory
            {
                PhoneNumber = phoneNumber,
                PreferredLanguage = preferredLanguage ?? "fr-CA",
                LastDepartment = lastDepartment ?? string.Empty,
                LastAdvisorName = lastAdvisorName ?? string.Empty,
                LastIntentId = lastIntentId ?? string.Empty,
                VehicleYear = vehicleYear ?? string.Empty,
                VehicleModel = vehicleModel ?? string.Empty,
                LastSeenUtc = DateTime.UtcNow
            },
            (_, existing) =>
            {
                if (!string.IsNullOrWhiteSpace(preferredLanguage))
                    existing.PreferredLanguage = preferredLanguage;

                if (!string.IsNullOrWhiteSpace(lastDepartment))
                    existing.LastDepartment = lastDepartment;

                if (!string.IsNullOrWhiteSpace(lastAdvisorName))
                    existing.LastAdvisorName = lastAdvisorName;

                if (!string.IsNullOrWhiteSpace(lastIntentId))
                    existing.LastIntentId = lastIntentId;

                if (!string.IsNullOrWhiteSpace(vehicleYear))
                    existing.VehicleYear = vehicleYear;

                if (!string.IsNullOrWhiteSpace(vehicleModel))
                    existing.VehicleModel = vehicleModel;

                existing.LastSeenUtc = DateTime.UtcNow;

                return existing;
            });

        return updated;
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");
    }
}