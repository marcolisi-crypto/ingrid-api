namespace AIReception.Mvc.Models.Voice;

public class OutboundCallRequest
{
    public string To { get; set; } = "";
    public string Mode { get; set; } = "manual-softphone";

    public string? AgentIdentity { get; set; }
    public string? ScriptId { get; set; }
    public string? CampaignId { get; set; }
    public string? LeadId { get; set; }
    public string? ContactId { get; set; }
    public string? Department { get; set; }
    public string? InitiatedBy { get; set; }

    public string? Source { get; set; }
    public string? Notes { get; set; }
}
