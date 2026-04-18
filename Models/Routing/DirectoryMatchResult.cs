using AIReception.Mvc.Models.Directory;

namespace AIReception.Mvc.Models.Routing;

public class DirectoryMatchResult
{
    public DirectoryEntry Entry { get; set; } = null!;
    public double Score { get; set; }
    public string MatchedOn { get; set; } = string.Empty;
}
