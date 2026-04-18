using System.Text.Json;
using AIReception.Mvc.Models.Directory;
using AIReception.Mvc.Models.Routing;

namespace AIReception.Mvc.Services;

public class DirectoryRoutingService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DirectoryRoutingService> _logger;
    private List<DirectoryEntry>? _cache;

    public DirectoryRoutingService(
        IWebHostEnvironment environment,
        ILogger<DirectoryRoutingService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public List<DirectoryEntry> GetAll()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var path = Path.Combine(_environment.ContentRootPath, "Data", "ai_receptionist_final_directory.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("Directory file not found at {Path}", path);
            _cache = new List<DirectoryEntry>();
            return _cache;
        }

        var json = File.ReadAllText(path);

        _cache = JsonSerializer.Deserialize<List<DirectoryEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<DirectoryEntry>();

        ValidateEntries(_cache);
        return _cache;
    }

    public DirectoryEntry? FindMatch(string input)
    {
        return FindBestMatch(input)?.Entry;
    }

    public DirectoryEntry? FindDepartment(string department)
    {
        var normalized = Normalize(department);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var departments = GetAll()
            .Where(entry => string.Equals(entry.Type, "department", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var exactDepartment = departments.FirstOrDefault(entry =>
            string.Equals(Normalize(entry.Department), normalized, StringComparison.OrdinalIgnoreCase));

        if (exactDepartment != null)
        {
            return exactDepartment;
        }

        var exactName = departments.FirstOrDefault(entry =>
            string.Equals(Normalize(entry.Name), normalized, StringComparison.OrdinalIgnoreCase));

        if (exactName != null)
        {
            return exactName;
        }

        var exactKeyword = departments.FirstOrDefault(entry =>
            entry.Keywords.Any(keyword =>
                string.Equals(Normalize(keyword), normalized, StringComparison.OrdinalIgnoreCase)));

        if (exactKeyword != null)
        {
            return exactKeyword;
        }

        var partialDepartment = departments.FirstOrDefault(entry =>
            Normalize(entry.Department).Contains(normalized) ||
            normalized.Contains(Normalize(entry.Department)));

        if (partialDepartment != null)
        {
            return partialDepartment;
        }

        var partialName = departments.FirstOrDefault(entry =>
            Normalize(entry.Name).Contains(normalized) ||
            normalized.Contains(Normalize(entry.Name)));

        if (partialName != null)
        {
            return partialName;
        }

        var partialKeyword = departments.FirstOrDefault(entry =>
            entry.Keywords.Any(keyword =>
                Normalize(keyword).Contains(normalized) ||
                normalized.Contains(Normalize(keyword))));

        return partialKeyword;
    }

    public DirectoryEntry? FindEmployee(string input)
    {
        return FindBestEmployeeMatch(input)?.Entry;
    }

    public EmployeeMatchResult? FindBestEmployeeMatch(string input)
    {
        var normalized = Normalize(input);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 1)
            .ToArray();

        var results = new List<EmployeeMatchResult>();

        foreach (var entry in GetAll().Where(e =>
                     string.Equals(e.Type, "employee", StringComparison.OrdinalIgnoreCase)))
        {
            double score = 0;
            var matchedOn = new List<string>();

            var normalizedName = Normalize(entry.Name);
            var normalizedKeywords = entry.Keywords
                .Select(Normalize)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            // Strongest match: full normalized name appears in input
            if (!string.IsNullOrWhiteSpace(normalizedName) &&
                normalized.Contains(normalizedName))
            {
                score += 3.0;
                matchedOn.Add($"name:{entry.Name}");
            }

            // Reverse full-name containment
            if (!string.IsNullOrWhiteSpace(normalizedName) &&
                normalizedName.Contains(normalized))
            {
                score += 2.0;
                matchedOn.Add($"reverse-name:{entry.Name}");
            }

            // Exact keyword phrase inside input
            foreach (var keyword in normalizedKeywords)
            {
                if (normalized.Contains(keyword))
                {
                    score += 2.0;
                    matchedOn.Add($"keyword:{keyword}");
                }
            }

            // Token-level matching
            foreach (var token in tokens)
            {
                if (normalizedName.Contains(token))
                {
                    score += 0.8;
                    matchedOn.Add($"name-token:{token}");
                }

                if (normalizedKeywords.Any(k => k.Contains(token)))
                {
                    score += 0.8;
                    matchedOn.Add($"keyword-token:{token}");
                }
            }

            // Small bonus if the last token strongly matches a surname/unique token
            if (tokens.Length > 0)
            {
                var lastToken = tokens[^1];
                if (normalizedName.Contains(lastToken))
                {
                    score += 0.5;
                    matchedOn.Add($"last-token:{lastToken}");
                }
            }

            if (score > 0)
            {
                results.Add(new EmployeeMatchResult
                {
                    Entry = entry,
                    Score = score,
                    MatchedOn = string.Join(", ", matchedOn.Distinct())
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .FirstOrDefault();
    }

    public DirectoryMatchResult? FindBestMatch(string input)
    {
        var normalized = Normalize(input);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var results = new List<DirectoryMatchResult>();

        foreach (var entry in GetAll())
        {
            double score = 0;
            var matchedOn = "";

            var normalizedName = Normalize(entry.Name);
            var normalizedDepartment = Normalize(entry.Department);

            if (!string.IsNullOrWhiteSpace(normalizedName) && normalized.Contains(normalizedName))
            {
                score += 1.0;
                matchedOn = $"name:{entry.Name}";
            }

            if (!string.IsNullOrWhiteSpace(normalizedDepartment) && normalized.Contains(normalizedDepartment))
            {
                score += 0.3;
                if (string.IsNullOrWhiteSpace(matchedOn))
                    matchedOn = $"department:{entry.Department}";
            }

            foreach (var keyword in entry.Keywords)
            {
                var normalizedKeyword = Normalize(keyword);
                if (string.IsNullOrWhiteSpace(normalizedKeyword))
                    continue;

                if (normalized.Contains(normalizedKeyword))
                {
                    score += 0.2;
                    if (string.IsNullOrWhiteSpace(matchedOn))
                        matchedOn = $"keyword:{keyword}";
                }
            }

            if (score > 0)
            {
                results.Add(new DirectoryMatchResult
                {
                    Entry = entry,
                    Score = score,
                    MatchedOn = matchedOn
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.Entry.Type == "employee")
            .FirstOrDefault();
    }

    private void ValidateEntries(List<DirectoryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                _logger.LogWarning("Directory entry missing name");
            }

            if (string.IsNullOrWhiteSpace(entry.PhoneE164))
            {
                _logger.LogWarning("Directory entry {Name} missing phone_e164", entry.Name);
            }
            else if (!entry.PhoneE164.StartsWith("+", StringComparison.Ordinal))
            {
                _logger.LogWarning("Directory entry {Name} has invalid E.164 number: {Phone}", entry.Name, entry.PhoneE164);
            }
        }
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = new string(
            value
                .ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

        var stopWords = new HashSet<string>
        {
            "i", "would", "like", "to", "speak", "with", "please", "can", "could",
            "talk", "me", "want", "wanna", "need", "connect", "transfer",
            "je", "voudrais", "parler", "avec", "svp", "sil", "vous", "plait",
            "aimerais", "joindre"
        };

        var cleanedWords = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => !stopWords.Contains(w));

        return string.Join(" ", cleanedWords).Trim();
    }
}

public class EmployeeMatchResult
{
    public DirectoryEntry Entry { get; set; } = new();
    public double Score { get; set; }
    public string MatchedOn { get; set; } = string.Empty;
}
